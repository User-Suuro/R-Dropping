Imports MySql.Data.MySqlClient

Public Class PricingPage
    Inherits BasePanel
    Implements IRefreshable

    Private routeName As String = "Pricing"

    Private _dgv As BaseDGV

    Public Sub RefreshPage() Implements IRefreshable.Refresh
        FetchData()
    End Sub

    Public Sub New()
        Me.Dock = DockStyle.Fill
        InitializeComponent()
        SetupEventHandlers()
        FetchData()
        root.RootInstance.SetRouteLabel(routeName)
        AddHandler _dgv.AfterPageApplied, Sub(s, e) HandleCol()
    End Sub

    Private Sub InitializeComponent()
        _dgv = New BaseDGV()
        InitializeActionBtn()
        Me.Controls.Add(_dgv)
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
                                                Dim filterQuery = $"{Pricing.rate_label} LIKE '%{searchText}%'"
                                                _dgv.FilterData(searchText, filterQuery)
                                                HandleCol()
                                            End Sub


        AddHandler _addBtn.Click, Sub(sender, e)
                                      root.rootNav.GoToPage(New PricingForm())
                                  End Sub

        AddHandler _updateBtn.Click, Sub(sender, e)
                                         Dim selectedRow = _dgv.GetSelectedRow()
                                         root.rootNav.GoToPage(New PricingForm(selectedRow.Cells(0).Value))
                                     End Sub

        AddHandler _deleteBtn.Click, Sub(sender, e)
                                         Dim selectedRow = _dgv.GetSelectedRow()
                                         DeleteData(selectedRow.Cells(0).Value)
                                     End Sub

        AddHandler _refreshBtn.Click, Sub(sender, e)
                                          FetchData()
                                      End Sub
    End Sub

    Private Async Sub FetchData()

        Dim loadingDlg As New BaseDialog()

        Await DialogTypes.ShowLoadingUntilAsync(
            loadingDlg,
            Form1.Instance,
            Async Function()
                Dim sql As String = $"SELECT * FROM {Pricing.table_name}"

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
        If _dgv.DataGridView.Columns.Contains(Pricing.id) Then
            _dgv.DataGridView.Columns(Pricing.id).Visible = False
        End If
    End Sub

    Public Async Sub DeleteData(id As Integer)
        Dim confirm_dlg = New BaseDialog()

        DialogTypes.Apply(confirm_dlg,
                 DialogType.Confirmation,
                 "Confirmation",
                 "Are you sure you want to delete this pricing rate?")

        If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
            Dim sql As String = $"DELETE FROM {Pricing.table_name} 
                                WHERE {Pricing.id} = @{Pricing.id}"

            Dim params As New Dictionary(Of String, Object) From {
                {$"@{Pricing.id}", id}
            }

            Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)

            If affectedRows > 0 Then

                Dim info_dlg = New BaseDialog()

                DialogTypes.Apply(info_dlg,
                          DialogType.Info,
                          "Success",
                          "Pricing rate was deleted successfully")

                Await info_dlg.ShowBaseDialogAsync(Form1.Instance)

                FetchData()

            End If
        End If

        ' Handle error

    End Sub
End Class
