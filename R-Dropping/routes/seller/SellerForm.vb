Imports MySql.Data.MySqlClient

Public Class SellerForm
    Inherits BasePanel

    Private _subContainer As PrimaryFlowLayoutPanel

    Private _sellerNameInput As BaseInputPanel
    Private _sellerNameField As ValidationPanel

    Private _sellerEmailInput As BaseInputPanel
    Private _sellerEmailField As ValidationPanel

    Private _contactNoInput As BaseInputPanel
    Private _contactNoField As ValidationPanel

    Private _platformInput As BaseComboBox
    Private _platformField As ValidationPanel

    Private _addButton As BaseButton
    Private _cancelButton As BaseButton

    Private _buttonTable As TableLayoutPanel
    Private _id As Integer?

    Public Sub New(Optional id As Integer? = Nothing)
        Me.Dock = DockStyle.Fill
        _id = id
        InitializeComponent()
        _subContainer.Visible = False
        AddHandler Me.Resize, AddressOf CenterSubContainer
        AddHandler _subContainer.SizeChanged, AddressOf CenterSubContainer
        CenterSubContainer(Nothing, EventArgs.Empty)
        LoadAsync()
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

        ' Seller Name
        _sellerNameInput = New BaseInputPanel With {
            .LabelText = "Seller Name"
        }

        _sellerNameField = New ValidationPanel(_sellerNameInput)
        _sellerNameField.SetValidator(New InputValidator().Required().NoSpecialChar())

        ' Email

        _sellerEmailInput = New BaseInputPanel() With {
            .LabelText = "Email (optional)"
        }

        _sellerEmailField = New ValidationPanel(_sellerEmailInput)
        _sellerEmailField.SetValidator(New InputValidator().IsEmail().NoSpecialChar())

        ' Contact No

        _contactNoInput = New BaseInputPanel() With {
            .LabelText = "Contact Number (optional)"
        }

        _contactNoField = New ValidationPanel(_contactNoInput)
        _contactNoField.SetValidator(New InputValidator().IsPhone().NoSpecialChar())

        ' Platform
        _platformInput = New BaseComboBox("Platform") With {
            .Placeholder = "Select platform type...",
            .SearchEnabled = False,
            .Items = New List(Of String) From {
                "Facebook", "TikTok"
            }
        }

        _platformField = New ValidationPanel(_platformInput)
        _platformField.SetValidator(New InputValidator().Required().NoSpecialChar())

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

        ' Controls

        _buttonTable.Controls.Add(_cancelButton, 0, 0)
        _buttonTable.Controls.Add(_addButton, 1, 0)

        With _subContainer.Controls
            .Add(_sellerNameField)
            .Add(_sellerEmailField)
            .Add(_contactNoField)
            .Add(_platformField)
            .Add(_buttonTable)
        End With

        Me.Controls.Add(_subContainer)

        ' Bind Event

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

        Dim msg As String = "Are you sure you want to add this seller?"

        If _id.HasValue() Then
            msg = "Are you sure you want to save changes to this seller?"
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
        Return {_sellerNameField, _sellerEmailField, _contactNoField, _platformField}.All(Function(f) f.ValidateInput())
    End Function

    Private Async Function AddQuery() As Task(Of Boolean)
        Dim sql As String =
        $"INSERT INTO {Seller.table_name} " &
        $"({Seller.seller_name}, {Seller.email}, {Seller.contact_no}, {Seller.platform}) " &
        $"VALUES (@{Seller.seller_name}, @{Seller.email}, @{Seller.contact_no}, @{Seller.platform})"

        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Seller.seller_name}", _sellerNameInput.Value},
        {$"@{Seller.email}", _sellerEmailInput.Value.Trim())},
        {$"@{Seller.contact_no}", ToDbNull(_contactNoInput.Value.Trim())},
        {$"@{Seller.platform}", _platformInput.SelectedValue}
        }

        Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)

        If affectedRows > 0 Then
            Return True
        End If

        Return False
    End Function


    Private Async Sub LoadAsync()
        ' Await loadDataForCmb()
        If _id.HasValue() Then
            Await fetchDataForEditMode(_id.Value)
        End If
    End Sub

    Private Async Function fetchDataForEditMode(id As Integer) As Task
        _addButton.Text = "Save"

        Dim sql As String =
        $"SELECT {Seller.seller_name}, {Seller.email}, {Seller.contact_no}, {Seller.platform} " &
        $"FROM {Seller.table_name} " &
        $"WHERE {Seller.id} = @{Seller.id}"

        Dim params As New Dictionary(Of String, Object) From {
            {$"@{Seller.id}", id}
        }

        Dim reader As MySqlDataReader = Await ReadQueryAsync(sql, params)

        If reader IsNot Nothing Then
            While Await reader.ReadAsync()
                Dim sellerName As String = reader(Seller.seller_name)
                Dim sellerEmail As String = If(IsDBNull(reader(Seller.email)), "", reader(Seller.email))
                Dim contactNumber As String = If(IsDBNull(reader(Seller.contact_no)), "", reader(Seller.contact_no))
                Dim platform As String = reader(Seller.platform)

                _sellerNameInput.SetValue(sellerName)
                _sellerEmailInput.SetValue(sellerEmail)
                _contactNoInput.SetValue(contactNumber)
                _platformInput.SetValue(platform)
            End While

            reader.Close()
        End If
    End Function

    Private Async Function EditQuery() As Task(Of Boolean)
        Dim sql As String =
       $"UPDATE {Seller.table_name} SET " &
       $"{Seller.seller_name} = @{Seller.seller_name}, " &
       $"{Seller.email} = @{Seller.email}, " &
       $"{Seller.contact_no} = @{Seller.contact_no}, " &
       $"{Seller.platform} = @{Seller.platform} " &
       $"WHERE {Seller.id} = @{Seller.id}"

        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Seller.seller_name}", _sellerEmailInput.Value},
        {$"@{Seller.email}", ToDbNull(_sellerEmailInput.Value.Trim())},
        {$"@{Seller.contact_no}", ToDbNull(_contactNoInput.Value.Trim())},
        {$"@{Seller.platform}", _platformInput.SelectedValue},
        {$"@{Seller.id}", _id}
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
        _subContainer.Visible = True
    End Sub
End Class
