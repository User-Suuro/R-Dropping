Imports MySql.Data.MySqlClient

Public Class BuyerForm
    Inherits BasePanel

    Private _subContainer As PrimaryFlowLayoutPanel

    Private _firstNameInput As BaseInputPanel
    Private _firstNameField As ValidationPanel

    Private _lastNameInput As BaseInputPanel
    Private _lastNameField As ValidationPanel

    Private _contactNoInput As BaseInputPanel
    Private _contactNoField As ValidationPanel

    Private _addressInput As BaseInputPanel
    Private _addressField As ValidationPanel

    Private _addButton As BaseButton
    Private _cancelButton As BaseButton

    Private _buttonTable As TableLayoutPanel
    Private _id As Integer?

    Public Sub New(Optional id As Integer? = Nothing)
        Me.Dock = DockStyle.Fill
        _id = id
        InitializeComponent()
        AddHandler Me.Resize, AddressOf CenterSubContainer
        AddHandler _subContainer.SizeChanged, AddressOf CenterSubContainer
    End Sub

    Public Sub InitializeComponent()
        _subContainer = New PrimaryFlowLayoutPanel() With {
            .AutoSize = True,
            .FlowDirection = FlowDirection.TopDown,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .BackColor = Color.White,
            .Padding = New Padding(16),
            .BorderStyle = BorderStyle.FixedSingle
        }

        ' First Name
        _firstNameInput = New BaseInputPanel() With {
            .LabelText = "First Name"
        }

        _firstNameField = New ValidationPanel(_firstNameInput)
        _firstNameField.SetValidator(New InputValidator().Required().NoSpecialChar())


        ' Last Name
        _lastNameInput = New BaseInputPanel() With {
            .LabelText = "Last Name"
        }

        _lastNameField = New ValidationPanel(_lastNameInput)
        _lastNameField.SetValidator(New InputValidator().Required().NoSpecialChar())

        ' Contact No
        _contactNoInput = New BaseInputPanel() With {
            .LabelText = "Contact Number (optional)"
        }

        _contactNoField = New ValidationPanel(_contactNoInput)
        _contactNoField.SetValidator(New InputValidator().IsPhone().NoSpecialChar())

        ' Address
        _addressInput = New BaseInputPanel() With {
            .LabelText = "Address (optional)"
        }

        _addressField = New ValidationPanel(_addressInput)
        _addressField.SetValidator(New InputValidator().NoSpecialChar())


        ' Button Table
        _buttonTable = New TableLayoutPanel With {
             .ColumnCount = 2,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
             .Width = _subContainer.Width + 8,
            .Height = 40
        }

        _buttonTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        _buttonTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

        _addButton = New BaseButton() With {
            .Text = "Add",
            .Dock = DockStyle.Top
        }

        _addButton.SetPrimary()

        _cancelButton = New BaseButton() With {
            .Text = "Cancel",
            .Dock = DockStyle.Top
        }

        _cancelButton.SetDanger()

        ' Handle Edit Mode
        If _id.HasValue() Then
            handleEditMode(_id)
        End If

        ' Controls

        _buttonTable.Controls.Add(_cancelButton, 0, 0)
        _buttonTable.Controls.Add(_addButton, 1, 0)

        With _subContainer.Controls
            .Add(_firstNameField)
            .Add(_lastNameField)
            .Add(_contactNoField)
            .Add(_addressField)
            .Add(_buttonTable)
        End With

        Me.Controls.Add(_subContainer)

        ' Bind Events

        AddHandler _addButton.Click, AddressOf QueryBuyer
        AddHandler _cancelButton.Click, AddressOf CancelAdd
    End Sub

    Private Sub CancelAdd()
        root.rootNav.GoBackPage()
    End Sub

    Private Async Sub QueryBuyer()

        If Not ValidateAllInputs() Then
            Exit Sub
        End If

        Dim confirm_dlg = New BaseDialog()

        Dim msg As String = "Are you sure you want to add this buyer?"

        If _id.HasValue() Then
            msg = "Are you sure you want to save changes to this buyer?"
        End If

        DialogTypes.Apply(confirm_dlg,
                 DialogType.Confirmation,
                 "Confirmation",
                 msg)

        If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
            Dim loadingDlg As New BaseDialog()

            Dim completed As Boolean = Await DialogTypes.ShowLoadingUntilAsync(
                loadingDlg,
                Form1.Instance,
                Async Function()

                    Dim queryResult As Boolean

                    If _id.HasValue() Then
                        queryResult = Await EditQuery()
                    Else
                        queryResult = Await AddQuery()
                    End If

                    If queryResult Then
                        Dim info_dlg = New BaseDialog()

                        DialogTypes.Apply(info_dlg,
                          DialogType.Info,
                          "Success",
                          "Changes was saved successfully")

                        Dim result_info_dlg = Await info_dlg.ShowBaseDialogAsync(Form1.Instance)

                        If result_info_dlg = DialogResultType.Confirm Then
                            root.rootNav.GoBackPage()
                        End If
                    Else
                        Dim error_dlg = New BaseDialog()

                        DialogTypes.Apply(error_dlg,
                          DialogType.Error,
                          "Error",
                          "Something went wrong")

                        error_dlg.ShowDialog()
                    End If
                    
                End Function
            )
        Else
            confirm_dlg.Hide()
        End If

    End Sub

    Private Function ValidateAllInputs() As Boolean
        Return {_firstNameField, _lastNameField, _addressField, _contactNoField}.All(Function(f) f.ValidateInput())
    End Function

    Private Async Function AddQuery() As Task(Of Boolean)
        Dim sql As String =
        $"INSERT INTO {Buyer.table_name} " &
        $"({Buyer.first_name}, {Buyer.last_name}, {Buyer.contact_no}, {Buyer.address}) " &
        $"VALUES (@{Buyer.first_name}, @{Buyer.last_name}, @{Buyer.contact_no}, @{Buyer.address})"

        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Buyer.first_name}", _firstNameInput.Value},
        {$"@{Buyer.last_name}", _lastNameInput.Value},
        {$"@{Buyer.contact_no}", ToDbNull(_contactNoInput.Value)},
        {$"@{Buyer.address}", ToDbNull(_addressInput.Value)}
        }

        Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)

        If affectedRows > 0 Then
            Return True
        End If

        Return False
    End Function

    Private Async Sub handleEditMode(id As Integer)
        _addButton.Text = "Save"

        Dim sql As String =
       $"SELECT {Buyer.first_name}, {Buyer.last_name}, {Buyer.address}, {Buyer.contact_no} " &
       $"FROM {Buyer.table_name} " &
       $"WHERE {Buyer.id} = @{Buyer.id}"

        Dim params As New Dictionary(Of String, Object) From {
                {$"@{Buyer.id}", id}
        }

        Dim reader As MySqlDataReader = Await ReadQueryAsync(sql, params)

        If reader IsNot Nothing Then
            While Await reader.ReadAsync()
                Dim firstName As String = reader(Buyer.first_name).ToString()
                Dim lastName As String = reader(Buyer.last_name).ToString()
                Dim address As String = If(IsDBNull(reader(Buyer.address)), "", reader(Buyer.address).ToString())
                Dim contact_no As String = If(IsDBNull(reader(Buyer.contact_no)), "", reader(Buyer.contact_no).ToString())

                _firstNameInput.SetValue(firstName)
                _lastNameInput.SetValue(lastName)
                _contactNoInput.SetValue(contact_no)
                _addressInput.SetValue(address)
            End While

            reader.Close()
        End If
    End Sub

    Private Async Function EditQuery() As Task(Of Boolean)
        Dim sql As String =
       $"UPDATE {Buyer.table_name} SET " &
       $"{Buyer.first_name} = @{Buyer.first_name}, " &
       $"{Buyer.last_name} = @{Buyer.last_name}, " &
       $"{Buyer.contact_no} = @{Buyer.contact_no}, " &
       $"{Buyer.address} = @{Buyer.address} " &
       $"WHERE {Buyer.id} = @{Buyer.id}"


        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Buyer.first_name}", _firstNameInput.Value},
        {$"@{Buyer.last_name}", _lastNameInput.Value},
        {$"@{Buyer.contact_no}", ToDbNull(_contactNoInput.Value)},
        {$"@{Buyer.address}", ToDbNull(_addressInput.Value)},
        {$"@{Buyer.id}", _id}
        }
        Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)
        If affectedRows > 0 Then
            Return True
        End If
        Return False
    End Function


    Private Sub CenterSubContainer(sender As Object, e As EventArgs)
        _subContainer.Left = (Me.ClientSize.Width - _subContainer.Width) \ 2
        _subContainer.Top = (Me.ClientSize.Height - _subContainer.Height) \ 2
    End Sub
End Class
