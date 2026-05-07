Imports MySql.Data.MySqlClient

Public Class EmployeeForm
    Inherits BasePanel

    Private _subContainer As PrimaryFlowLayoutPanel

    Private _inpFirstName As BaseInputPanel
    Private _fieldFirstName As ValidationPanel

    Private _inpLastName As BaseInputPanel
    Private _fieldLastName As ValidationPanel

    Private _inpMiddleName As BaseInputPanel
    Private _fieldMiddleName As ValidationPanel

    Private _cbxPosition As BaseComboBox
    Private _cbxPosField As ValidationPanel

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
        _inpFirstName = New BaseInputPanel() With {
            .LabelText = "First Name"
        }

        _fieldFirstName = New ValidationPanel(_inpFirstName)
        _fieldFirstName.SetValidator(New InputValidator().Required().NoSpecialChar())

        ' Middle Name
        _inpMiddleName = New BaseInputPanel() With {
            .LabelText = "Middle Name (optional)"
        }

        _fieldMiddleName = New ValidationPanel(_inpMiddleName)
        _fieldMiddleName.SetValidator(New InputValidator().NoSpecialChar())

        ' Last Name
        _inpLastName = New BaseInputPanel() With {
            .LabelText = "Last Name"
        }

        _fieldLastName = New ValidationPanel(_inpLastName)
        _fieldLastName.SetValidator(New InputValidator().Required().NoSpecialChar())

        ' Position
        _cbxPosition = New BaseComboBox("Position") With {
            .Placeholder = "Select position...",
            .SearchEnabled = False,
            .Items = New List(Of String) From {
              "Admin", "Manager", "Staff"
            }
        }

        _cbxPosField = New ValidationPanel(_cbxPosition)
        _cbxPosField.SetValidator(New InputValidator().Required())

        ' Button Table
        _buttonTable = New TableLayoutPanel With {
             .ColumnCount = 2,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Width = _fieldLastName.Width + 8,
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
            .Add(_fieldFirstName)
            .Add(_fieldMiddleName)
            .Add(_fieldLastName)
            .Add(_cbxPosField)
            .Add(_buttonTable)
        End With

        Me.Controls.Add(_subContainer)

        ' Bind Events

        AddHandler _addButton.Click, AddressOf QueryEmployee
        AddHandler _cancelButton.Click, AddressOf CancelAdd
    End Sub

    Private Async Sub handleEditMode(id As Integer)
        _addButton.Text = "Save"

        Dim sql As String =
        $"SELECT {Employee.first_name}, {Employee.last_name}, {Employee.middle_name}, {Employee.position} " &
        $"FROM {Employee.table_name} " &
        $"WHERE {Employee.id} = @{Employee.id}"

        Dim params As New Dictionary(Of String, Object) From {
                {$"@{Employee.id}", id}
        }

        Dim reader As MySqlDataReader = Await ReadQueryAsync(sql, params)

        If reader IsNot Nothing Then
            While Await reader.ReadAsync()
                Dim firstName As String = reader(Employee.first_name).ToString()
                Dim lastName As String = reader(Employee.last_name).ToString()
                Dim middleName As String = If(IsDBNull(reader(Employee.middle_name)), "", reader(Employee.middle_name).ToString())
                Dim position As String = reader(Employee.position).ToString()

                _inpFirstName.SetValue(firstName)
                _inpLastName.SetValue(lastName)
                _inpMiddleName.SetValue(middleName)
                _cbxPosition.SetValue(position)
            End While

            reader.Close()
        End If
    End Sub

    Private Function ValidateAllInputs() As Boolean
        Return {_fieldFirstName, _fieldMiddleName, _fieldLastName, _cbxPosField}.All(Function(f) f.ValidateInput())
    End Function

    Private Async Sub QueryEmployee()

        If Not ValidateAllInputs() Then
            Exit Sub
        End If

        Dim confirm_dlg = New BaseDialog()

        Dim msg As String = "Are you sure you want to add this employee?"

        If _id.HasValue() Then
            msg = "Are you sure you want to save changes to this employee?"
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
                        queryResult = Await EditEmployeeQuery()
                    Else
                        queryResult = Await AddEmployeeQuery()
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
                    End If
                End Function
            )
        Else
        End If

    End Sub

    Private Async Function AddEmployeeQuery() As Task(Of Boolean)
        Dim sql As String =
        $"INSERT INTO {Employee.table_name} " &
        $"({Employee.first_name}, {Employee.middle_name}, {Employee.last_name}, {Employee.position}) " &
        $"VALUES (@{Employee.first_name}, @{Employee.middle_name}, @{Employee.last_name}, @{Employee.position})"

        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Employee.first_name}", _inpFirstName.Value},
        {$"@{Employee.middle_name}", ToDbNull(_inpMiddleName.Value)},
        {$"@{Employee.last_name}", _inpLastName.Value},
        {$"@{Employee.position}", _cbxPosition.SelectedValue}
        }

        Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)

        If affectedRows > 0 Then
            Return True
        End If

        Return False
    End Function

    Private Async Function EditEmployeeQuery() As Task(Of Boolean)
        Dim sql As String =
        $"UPDATE {Employee.table_name} SET " &
        $"{Employee.first_name} = @{Employee.first_name}, " &
        $"{Employee.middle_name} = @{Employee.middle_name}, " &
        $"{Employee.last_name} = @{Employee.last_name}, " &
        $"{Employee.position} = @{Employee.position} " &
        $"WHERE {Employee.id} = @{Employee.id}"

        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Employee.first_name}", _inpFirstName.Value},
        {$"@{Employee.middle_name}", ToDbNull(_inpMiddleName.Value)},
        {$"@{Employee.last_name}", _inpLastName.Value},
        {$"@{Employee.position}", _cbxPosition.SelectedValue},
        {$"@{Employee.id}", _id}
        }
        Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)
        If affectedRows > 0 Then
            Return True
        End If
        Return False
    End Function

    Private Sub CancelAdd()
        root.rootNav.GoBackPage()
    End Sub

    Private Sub CenterSubContainer(sender As Object, e As EventArgs)
        _subContainer.Left = (Me.ClientSize.Width - _subContainer.Width) \ 2
        _subContainer.Top = (Me.ClientSize.Height - _subContainer.Height) \ 2
        _subContainer.Visible = True
    End Sub

End Class
