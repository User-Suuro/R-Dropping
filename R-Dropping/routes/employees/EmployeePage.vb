
Imports MySql.Data.MySqlClient

Public Class EmployeePage
    Inherits BasePanel
    Implements IRefreshable

    Private _dgv As BaseDGV
    Private routeName As String = "Employees"

    Public Sub RefreshPage() Implements IRefreshable.Refresh
        FetchEmployeesData()
    End Sub

    Public Sub New()
        Me.Dock = DockStyle.Fill
        InitializeComponent()
        SetupEventHandlers()
        FetchEmployeesData()
    End Sub

    Private Sub InitializeComponent()
        _dgv = New BaseDGV()
        InitializeActionBtn()
        Me.Controls.Add(_dgv)
        root.RootInstance.SetRouteLabel(routeName)
        AddHandler _dgv.AfterPageApplied, Sub(s, e) HandleCol()
        AddHandler _dgv.DataGridView.SelectionChanged, AddressOf HandleButtonState
    End Sub


    Private _deleteBtn As BaseButton
    Private _updateBtn As BaseButton
    Private _addBtn As BaseButton
    Private _refreshBtn As BaseButton


    Private Sub InitializeActionBtn()

        _deleteBtn = New BaseButton With {
            .Text = "Delete",
            .Width = 95,
            .Height = 38,
            .Margin = New Padding(6, 0, 0, 0),
            .Visible = False
        }
        _deleteBtn.SetDanger()

        _updateBtn = New BaseButton With {
            .Text = "Update",
            .Width = 100,
            .Height = 38,
            .Margin = New Padding(6, 0, 0, 0),
            .Visible = False
        }
        _updateBtn.SetPrimary()

        _addBtn = New BaseButton With {
            .Text = "Add",
            .Width = 90,
            .Height = 38,
            .Margin = New Padding(6, 0, 0, 0)
        }
        _addBtn.SetPrimary()

        _refreshBtn = New BaseButton With {
            .Text = "Refresh",
            .Width = 105,
            .Height = 38,
            .Margin = Padding.Empty
        }
        _refreshBtn.SetPrimary()

        _dgv.AddActionButton(_deleteBtn, ButtonVisibility.OnSelection)
        _dgv.AddActionButton(_updateBtn, ButtonVisibility.OnSelection)
        _dgv.AddActionButton(_addBtn, ButtonVisibility.Always)
        _dgv.AddActionButton(_refreshBtn, ButtonVisibility.Always)
    End Sub

    Private Sub SetupEventHandlers()


        AddHandler _dgv.SearchButton.Click, Sub(sender, e)
                                                Dim searchText = _dgv.GetSearchText()
                                                Dim filterQuery = $"{Employee.first_name} LIKE '%{searchText}%' OR {Employee.last_name} LIKE '%{searchText}%'"
                                                _dgv.FilterData(searchText, filterQuery)
                                                HandleCol()
                                            End Sub


        AddHandler _addBtn.Click, Sub(sender, e)
                                      root.rootNav.GoToPage(New EmployeeForm())
                                  End Sub

        AddHandler _updateBtn.Click, Sub(sender, e)
                                         Dim selectedRow = _dgv.GetSelectedRow()
                                         root.rootNav.GoToPage(New EmployeeForm(selectedRow.Cells(0).Value))
                                     End Sub

        AddHandler _deleteBtn.Click, Sub(sender, e)
                                         Dim selectedRow = _dgv.GetSelectedRow()
                                         DeleteEmployees(selectedRow.Cells(0).Value)
                                     End Sub

        AddHandler _refreshBtn.Click, Sub(sender, e)
                                          FetchEmployeesData()
                                      End Sub
    End Sub

    Private Async Sub FetchEmployeesData()

        Dim loadingDlg As New BaseDialog()

        Await DialogTypes.ShowLoadingUntilAsync(
            loadingDlg,
            Form1.Instance,
            Async Function()
                Dim sql As String = $"SELECT {Employee.id}, {Employee.first_name}, {Employee.middle_name}, {Employee.position}, {Employee.email} FROM {Employee.table_name}"

                Dim reader As MySqlDataReader = Await ReadQueryAsync(sql)

                If reader IsNot Nothing Then
                    Dim dt As New DataTable()
                    dt.Load(reader)
                    reader.Close()

                    _dgv.BindDataSource(dt)
                End If

                HandleCol()
            End Function
        )
    End Sub

    Private Sub HandleCol()
        If _dgv.DataGridView.Columns.Contains(Employee.id) Then
            _dgv.DataGridView.Columns(Employee.id).Visible = False
        End If
    End Sub

    Public Async Sub DeleteEmployees(id As Integer)
        Dim confirm_dlg = New BaseDialog()

        DialogTypes.Apply(confirm_dlg,
                 DialogType.Confirmation,
                 "Confirmation",
                 "Are you sure you want to delete this employee?")

        If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
            Dim sql As String = $"DELETE FROM {Employee.table_name} 
                                WHERE {Employee.id} = @{Employee.id}"


            Dim params As New Dictionary(Of String, Object) From {
                {$"@{Employee.id}", id}
            }

            Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)

            If affectedRows > 0 Then

                Dim info_dlg = New BaseDialog()

                DialogTypes.Apply(info_dlg,
                          DialogType.Info,
                          "Success",
                          "Employee data was deleted successfully")

                Await info_dlg.ShowBaseDialogAsync(Form1.Instance)

                FetchEmployeesData()

            End If
        End If

        ' Handle error

    End Sub

    Private Sub HandleButtonState(sender As Object, e As EventArgs)

        If _dgv.DataGridView.SelectedRows.Count = 0 Then
            _updateBtn.Enabled = False
            _deleteBtn.Enabled = False
            Return
        End If

        Dim selectedRow = _dgv.GetSelectedRow()

        Dim selectedId As Integer =
        Convert.ToInt32(selectedRow.Cells(Employee.id).Value)

        Dim selectedPosition As String =
        selectedRow.Cells(Employee.position).Value.ToString().Trim()

        Dim currentPosition As String =
        session.SessionPosition.Trim()
        Dim isSameUser As Boolean =
        (selectedId = session.SessionUserID)

        Dim canManage As Boolean = False

        Select Case currentPosition.ToLower()

            Case "admin"


                If selectedPosition.Equals("Manager", StringComparison.OrdinalIgnoreCase) OrElse
               selectedPosition.Equals("Staff", StringComparison.OrdinalIgnoreCase) Then

                    canManage = True
                End If

            Case "manager"

                If selectedPosition.Equals("Staff", StringComparison.OrdinalIgnoreCase) Then
                    canManage = True
                End If

            Case Else


                canManage = False

        End Select

        ' Cannot manage yourself
        If isSameUser Then
            canManage = False
        End If

        _updateBtn.Enabled = canManage
        _deleteBtn.Enabled = canManage

    End Sub

End Class
