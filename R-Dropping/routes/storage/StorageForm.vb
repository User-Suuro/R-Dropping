Imports MySql.Data.MySqlClient

Public Class StorageForm
    Inherits BasePanel

    Private _subContainer As PrimaryFlowLayoutPanel

    Private _storageNameInp As BaseInputPanel
    Private _storageNameField As ValidationPanel

    Private _storageTypeCmb As InputComboBox
    Private _storageTypeField As ValidationPanel

    Private _capLimitInp As BaseNumericPanel
    Private _capLimitField As ValidationPanel

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

        ' Storage Name
        _storageNameInp = New BaseInputPanel With {
            .LabelText = "Storage Name"
        }
        _storageNameField = New ValidationPanel(_storageNameInp)
        _storageNameField.SetValidator(New InputValidator().Required().NoSpecialChar())

        ' Storage Type

        _storageTypeCmb = New InputComboBox("Storage Type")
        _storageTypeField = New ValidationPanel(_storageTypeCmb)
        _storageTypeField.SetValidator(New InputValidator().Required())

        ' Capacity Limit
        _capLimitInp = New BaseNumericPanel() With {
            .LabelText = "Estimated Capacity Limit"
        }

        _capLimitField = New ValidationPanel(_capLimitInp)
        _capLimitField.SetValidator(New InputValidator().Required().MinValue(1))

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
            .Add(_storageNameField)
            .Add(_storageTypeField)
            .Add(_capLimitField)
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
        Return {_storageNameField, _storageTypeField, _capLimitField}.All(Function(f) f.ValidateInput())
    End Function

    Private Async Function AddQuery() As Task(Of Boolean)
        Dim sql As String =
        $"INSERT INTO {Storage.table_name} " &
        $"({Storage.storage_name}, {Storage.capacity_limit}, {Storage.storage_type}) " &
        $"VALUES (@{Storage.storage_name}, @{Storage.capacity_limit}, @{Storage.storage_type})"

        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Storage.storage_name}", _storageNameInp.Value.Trim()},
        {$"@{Storage.storage_type}", _storageTypeCmb.GetValue()},
        {$"@{Storage.capacity_limit}", _capLimitInp.Value}
        }

        Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)

        If affectedRows > 0 Then
            Return True
        End If

        Return False
    End Function


    Private Async Function loadDataForCmb() As Task
        Dim list As New List(Of String)

        Dim sql As String =
        $"SELECT DISTINCT {Storage.storage_type} " &
        $"FROM {Storage.table_name}"

        Using reader As MySqlDataReader = Await ReadQueryAsync(sql)
            If reader IsNot Nothing Then
                While Await reader.ReadAsync()
                    list.Add(reader(Storage.storage_type).ToString())
                End While
            End If
        End Using

        _storageTypeCmb.Items = list

    End Function

    Private Async Sub LoadAsync()
        Await loadDataForCmb()
        If _id.HasValue() Then
            Await fetchDataForEditMode(_id.Value)
        End If
    End Sub

    Private Async Function fetchDataForEditMode(id As Integer) As Task
        _addButton.Text = "Save"
        _capLimitInp.Enabled = False

        Dim sql As String =
        $"SELECT {Storage.storage_name}, {Storage.storage_type}, {Storage.capacity_limit} " &
        $"FROM {Storage.table_name} " &
        $"WHERE {Storage.id} = @{Storage.id}"

        Dim params As New Dictionary(Of String, Object) From {
            {$"@{Storage.id}", id}
        }

        Dim reader As MySqlDataReader = Await ReadQueryAsync(sql, params)

        If reader IsNot Nothing Then
            While Await reader.ReadAsync()
                Dim storageName As String = reader(Storage.storage_name)
                Dim storageType As String = reader(Storage.storage_type)
                Dim capacityLimit = reader(Storage.capacity_limit)

                _storageNameInp.SetValue(storageName)
                _storageTypeCmb.SetValue(storageType)
                _capLimitInp.SetValue(capacityLimit)

            End While

            reader.Close()
        End If
    End Function

    Private Async Function EditQuery() As Task(Of Boolean)
        Dim sql As String =
       $"UPDATE {Storage.table_name} SET " &
       $"{Storage.storage_name} = @{Storage.storage_name}, " &
       $"{Storage.storage_type} = @{Storage.storage_type}, " &
       $"{Storage.capacity_limit} = @{Storage.capacity_limit} " &
       $"WHERE {Storage.id} = @{Storage.id}"

        Dim params As New Dictionary(Of String, Object) From {
        {$"@{Storage.storage_name}", _storageNameInp.Value.Trim()},
        {$"@{Storage.storage_type}", _storageTypeCmb.GetValue()},
        {$"@{Storage.capacity_limit}", _capLimitInp.Value},
        {$"@{Storage.id}", _id}
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
