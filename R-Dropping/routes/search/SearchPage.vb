Imports System.Diagnostics.Eventing
Imports MySql.Data.MySqlClient

Public Class SearchPage
    Inherits BasePanel

    Private routeName As String = "SQL Search"

    Private _dgv As BaseDGV

    Public Sub New()
        Me.Dock = DockStyle.Fill
        InitializeComponent()
        root.RootInstance.SetRouteLabel(routeName)
        AddHandler _dgv.AfterPageApplied, Sub(s, e) HandleCol()
    End Sub

    Private Sub InitializeComponent()
        _dgv = New BaseDGV()
        Me.Controls.Add(_dgv)
        SetupEventHandlers()
        Dim dt As New DataTable()
        _dgv.BindDataSource(dt)
    End Sub

    Private Sub SetupEventHandlers()


        AddHandler _dgv.SearchButton.Click, Sub(sender, e)
                                                Dim searchText = _dgv.GetSearchText()
                                                FetchData(searchText)
                                            End Sub
    End Sub

    Private Async Sub FetchData(searchQuery As String)
        Try
            Dim loadingDlg As New BaseDialog()

            Await DialogTypes.ShowLoadingUntilAsync(
                loadingDlg,
                Form1.Instance,
                Async Function()
                    Dim sql As String = searchQuery

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
        Catch ex As Exception
            MessageBox.Show(searchQuery)
        End Try

    End Sub

    Private Sub HandleCol()
        If _dgv.DataGridView.Columns.Contains(Seller.id) Then
            _dgv.DataGridView.Columns(Seller.id).Visible = False
        End If
    End Sub



End Class
