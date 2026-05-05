Imports MySql.Data.MySqlClient

Public Class CourierForm
    Inherits BasePanel

    Private _subContainer As PrimaryFlowLayoutPanel

    Private _firstNameInput As BaseInputPanel
    Private _firstNameField As ValidationPanel

    Private _lastNameInput As BaseInputPanel
    Private _lastNameField As ValidationPanel

    Private _vehicleTypeInput As BaseComboBox
    Private _vehicleTypeField As ValidationPanel

    Private _vehicleBrandInput As InputComboBox
    Private _vehicleBrandField As ValidationPanel

    Private _plateNoInput As BaseInputPanel
    Private _plateNoField As ValidationPanel

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

        ' Vehicle Type
        _vehicleTypeInput = New BaseComboBox("Vehicle Type") With {
            .Placeholder = "Select vehicle type...",
            .SearchEnabled = False,
            .Items = New List(Of String) From {
             "Motorcyle", "Tricycle", "Truck", "Car"
            }
        }

        _vehicleTypeField = New ValidationPanel(_vehicleTypeInput)
        _vehicleTypeField.SetValidator(New InputValidator().Required())

        ' Vehicle Brand
        _vehicleBrandInput = New InputComboBox("Vehicle Brand")

        _vehicleBrandField = New ValidationPanel(_vehicleBrandInput)
        _vehicleBrandField.SetValidator(New InputValidator().Required())

        ' Plate Number
        _plateNoInput = New BaseInputPanel() With {
            .LabelText = "Plate Number"
        }
        _plateNoField = New ValidationPanel(_plateNoInput)
        _plateNoField.SetValidator(New InputValidator().Required().NoSpecialChar())


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
            .Add(_firstNameField)
            .Add(_lastNameField)
            .Add(_vehicleTypeField)
            .Add(_vehicleBrandField)
            .Add(_plateNoField)
            .Add(_buttonTable)
        End With

        Me.Controls.Add(_subContainer)

        ' Bind Event

        AddHandler _addButton.Click, AddressOf QueryBuyer
        AddHandler _cancelButton.Click, AddressOf CancelAdd
    End Sub

    Private Async Sub LoadAsync()
        Await loadDataForCmb()
        If _id.HasValue() Then
            Await fetchDataForEditMode(_id.Value)
        End If
    End Sub

    Private Sub CancelAdd()
        root.rootNav.GoBackPage()
    End Sub

    Private Async Sub QueryBuyer()

        If Not ValidateAllInputs() Then
            Exit Sub
        End If

        Dim confirm_dlg = New BaseDialog()

        Dim msg As String = "Are you sure you want to add this courier?"

        If _id.HasValue() Then
            msg = "Are you sure you want to save changes to this courier?"
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



    Private Async Function loadDataForCmb() As Task
        Dim list As New List(Of String)

        Dim sql As String =
        $"SELECT DISTINCT {Courier.vehicle_brand} " &
        $"FROM {Courier.table_name}"

        Using reader As MySqlDataReader = Await ReadQueryAsync(sql)
            If reader IsNot Nothing Then
                While Await reader.ReadAsync()
                    list.Add(reader(Courier.vehicle_brand).ToString())
                End While
            End If
        End Using

        _vehicleBrandInput.Items = list

    End Function

    Private Function ValidateAllInputs() As Boolean
        Return {_firstNameField, _lastNameField, _vehicleTypeField, _vehicleBrandField, _plateNoField}.All(Function(f) f.ValidateInput())
    End Function

    Private Async Function AddQuery() As Task(Of Boolean)
        Dim sql As String =
        $"INSERT INTO {Courier.table_name} " &
        $"({Courier.first_name}, {Courier.last_name}, {Courier.vehicle_type}, {Courier.vehicle_brand}, {Courier.plate_no}) " &
        $"VALUES (@{Courier.first_name}, @{Courier.last_name}, @{Courier.vehicle_type}, @{Courier.vehicle_brand}, @{Courier.plate_no})"

        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Courier.first_name}", _firstNameInput.Value},
        {$"@{Courier.last_name}", _lastNameInput.Value},
        {$"@{Courier.vehicle_type}", _vehicleTypeInput.SelectedValue},
        {$"@{Courier.vehicle_brand}", _vehicleBrandInput.GetValue()},
        {$"@{Courier.plate_no}", ToDbNull(_plateNoInput.Value)}
        }

        Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)

        If affectedRows > 0 Then
            Return True
        End If

        Return False
    End Function

    Private Async Function fetchDataForEditMode(id As Integer) As Task
        _addButton.Text = "Save"

        Dim sql As String =
       $"SELECT {Courier.first_name}, {Courier.last_name}, {Courier.vehicle_type}, {Courier.vehicle_brand}, {Courier.plate_no} " &
       $"FROM {Courier.table_name} " &
       $"WHERE {Courier.id} = @{Courier.id}"

        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Courier.id}", id}
    }

        Dim reader As MySqlDataReader = Await ReadQueryAsync(sql, params)

        If reader IsNot Nothing Then
            While Await reader.ReadAsync()
                Dim firstName As String = reader(Courier.first_name).ToString()
                Dim lastName As String = reader(Courier.last_name).ToString()
                Dim vehicle_type As String = reader(Courier.vehicle_type).ToString()
                Dim vehicle_brand As String = reader(Courier.vehicle_brand).ToString()
                Dim plate_no As String = reader(Courier.plate_no).ToString()

                _firstNameInput.SetValue(firstName)
                _lastNameInput.SetValue(lastName)
                _vehicleTypeInput.SetValue(vehicle_type)
                _vehicleBrandInput.SetValue(vehicle_brand)
                _plateNoInput.SetValue(plate_no)
            End While

            reader.Close()
        End If
    End Function

    Private Async Function EditQuery() As Task(Of Boolean)
        Dim sql As String =
       $"UPDATE {Courier.table_name} SET " &
       $"{Courier.first_name} = @{Courier.first_name}, " &
       $"{Courier.last_name} = @{Courier.last_name}, " &
       $"{Courier.vehicle_type} = @{Courier.vehicle_type}, " &
       $"{Courier.vehicle_brand} = @{Courier.vehicle_brand}, " &
       $"{Courier.plate_no} = @{Courier.plate_no} " &
       $"WHERE {Courier.id} = @{Courier.id}"


        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Courier.first_name}", _firstNameInput.Value},
        {$"@{Courier.last_name}", _lastNameInput.Value},
        {$"@{Courier.vehicle_type}", _vehicleTypeInput.SelectedValue},
        {$"@{Courier.vehicle_brand}", _vehicleBrandInput.GetValue()},
        {$"@{Courier.plate_no}", _plateNoInput.Value},
        {$"@{Courier.id}", _id}
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
