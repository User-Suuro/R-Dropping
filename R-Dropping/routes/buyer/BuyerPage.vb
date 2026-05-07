Imports System.IO
Imports MySql.Data.MySqlClient
Public Class BuyerPage
    Inherits BasePanel
    Implements IRefreshable

    Private routeName As String = "Buyers"

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
    Private _generateRevenueBtn As BaseButton

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

        _generateRevenueBtn = New BaseButton With {
            .Text = "See Revenue",
            .Width = 105,
            .Height = 38,
            .Margin = Padding.Empty
        }

        _generateRevenueBtn.SetPrimary()

        _dgv.AddActionButton(_deleteBtn, ButtonVisibility.OnSelection)
        _dgv.AddActionButton(_updateBtn, ButtonVisibility.OnSelection)
        _dgv.AddActionButton(_addBtn, ButtonVisibility.Always)
        _dgv.AddActionButton(_refreshBtn, ButtonVisibility.Always)
        _dgv.AddActionButton(_generateRevenueBtn, ButtonVisibility.OnSelection)
    End Sub

    Private Sub SetupEventHandlers()


        AddHandler _dgv.SearchButton.Click, Sub(sender, e)
                                                Dim searchText = _dgv.GetSearchText()
                                                Dim filterQuery = $"{Buyer.first_name} LIKE '%{searchText}%' OR {Buyer.last_name} LIKE '%{searchText}%'"
                                                _dgv.FilterData(searchText, filterQuery)
                                                HandleCol()
                                            End Sub
        AddHandler _generateRevenueBtn.Click,
            Async Sub(sender, e)

                Dim selectedRow = _dgv.GetSelectedRow()

                If selectedRow Is Nothing Then Return

                Dim buyerId As Integer =
                Convert.ToInt32(selectedRow.Cells(Buyer.id).Value)

                Dim confirm_dlg = New BaseDialog()

                DialogTypes.Apply(confirm_dlg,
                 DialogType.Confirmation,
                 "Confirmation",
                 "Are you sure you want to genarate revenue report for this buyer?")

                If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
                    Dim loadingDlg As New BaseDialog()

                    Await DialogTypes.ShowLoadingUntilAsync(
                        loadingDlg,
                        Form1.Instance,
                        Async Function()
                            Await GenerateSellerRevenueReport(buyerId)
                        End Function
                    )


                End If
            End Sub

        AddHandler _addBtn.Click, Sub(sender, e)
                                      root.rootNav.GoToPage(New BuyerForm())
                                  End Sub

        AddHandler _updateBtn.Click, Sub(sender, e)
                                         Dim selectedRow = _dgv.GetSelectedRow()
                                         root.rootNav.GoToPage(New BuyerForm(selectedRow.Cells(0).Value))
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
                Dim sql As String = $"SELECT * FROM {Buyer.table_name}"

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
        If _dgv.DataGridView.Columns.Contains(Buyer.id) Then
            _dgv.DataGridView.Columns(Buyer.id).Visible = False
        End If
    End Sub

    Public Async Sub DeleteData(id As Integer)
        Dim confirm_dlg = New BaseDialog()

        DialogTypes.Apply(confirm_dlg,
                 DialogType.Confirmation,
                 "Confirmation",
                 "Are you sure you want to delete this buyer?")

        If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
            Dim sql As String = $"DELETE FROM {Buyer.table_name} 
                                WHERE {Buyer.id} = @{Buyer.id}"


            Dim params As New Dictionary(Of String, Object) From {
                {$"@{Buyer.id}", id}
            }

            Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)

            If affectedRows > 0 Then

                Dim info_dlg = New BaseDialog()

                DialogTypes.Apply(info_dlg,
                          DialogType.Info,
                          "Success",
                          "Buyer data was deleted successfully")

                Await info_dlg.ShowBaseDialogAsync(Form1.Instance)

                FetchData()

            End If
        End If


    End Sub

    Private Async Function GenerateSellerRevenueReport(buyerId As Integer) As Task

        Try

            Dim relativePath As String =
            Config.BuildRelativePath(
                "reports",
                "seller_revenue",
                "seller_revenue_report",
                ".xlsx"
            )

            Dim fullPath As String =
            Path.Combine(
                Config.FindSolutionRoot(),
                relativePath
            )

            Directory.CreateDirectory(
                Path.GetDirectoryName(fullPath)
            )

            Dim collectedSql As String =
            $"SELECT
                i.{Item.name} AS item_name,
                i.{Item.item_price},
                i.{Item.drop_off_date},
                i.{Item.pickup_date}
            FROM {Item.table_name} i
            WHERE i.{Item.pickup_date} IS NOT NULL
            AND i.{Item.buyer_id} = @buyer_id
            ORDER BY i.{Item.pickup_date} DESC"

            Dim pendingSql As String =
            $"SELECT
                i.{Item.name} AS item_name,
                i.{Item.item_price},
                i.{Item.drop_off_date}
            FROM {Item.table_name} i
            WHERE i.{Item.pickup_date} IS NULL
            AND i.{Item.buyer_id} = @buyer_id
            ORDER BY i.{Item.drop_off_date} DESC"

            Dim params As New Dictionary(Of String, Object) From {
                {"@buyer_id", buyerId}
            }

            Using workbook As New ClosedXML.Excel.XLWorkbook()

                Dim ws = workbook.Worksheets.Add("Seller Revenue")

                ' ── Collected Revenue Section ──────────────────────────────

                Dim collectedHeaders As String() = {
                    "Item Name",
                    "Item Price",
                    "Drop Off Date",
                    "Pickup Date"
                }

                ws.Cell(1, 1).Value = "COLLECTED REVENUE"
                ws.Cell(1, 1).Style.Font.Bold = True

                For i As Integer = 0 To collectedHeaders.Length - 1
                    ws.Cell(2, i + 1).Value = collectedHeaders(i)
                    ws.Cell(2, i + 1).Style.Font.Bold = True
                Next

                Dim row As Integer = 3
                Dim grandTotal As Decimal = 0

                Using reader = Await ReadQueryAsync(collectedSql, params)
                    If reader IsNot Nothing Then
                        While Await reader.ReadAsync()

                            Dim itemName As String =
                                reader("item_name").ToString()

                            Dim itemPrice As Decimal =
                                Convert.ToDecimal(reader(Item.item_price))

                            Dim dropOffDate As DateTime =
                                Convert.ToDateTime(reader(Item.drop_off_date))

                            Dim pickupDate As DateTime =
                                Convert.ToDateTime(reader(Item.pickup_date))

                            grandTotal += itemPrice

                            ws.Cell(row, 1).Value = itemName
                            ws.Cell(row, 2).Value = itemPrice
                            ws.Cell(row, 3).Value = dropOffDate.ToString("MMMM dd, yyyy hh:mm tt")
                            ws.Cell(row, 4).Value = pickupDate.ToString("MMMM dd, yyyy hh:mm tt")

                            row += 1

                        End While
                    End If
                End Using

                ws.Cell(row + 1, 1).Value = "TOTAL REVENUE"
                ws.Cell(row + 1, 2).Value = grandTotal
                ws.Cell(row + 1, 1).Style.Font.Bold = True
                ws.Cell(row + 1, 2).Style.Font.Bold = True

                ' ── Pending Fee Section ────────────────────────────────────

                Dim pendingStartRow As Integer = row + 4

                Dim pendingHeaders As String() = {
                    "Item Name",
                    "Item Price",
                    "Drop Off Date"
                }

                ws.Cell(pendingStartRow, 1).Value = "PENDING ITEM FEE"
                ws.Cell(pendingStartRow, 1).Style.Font.Bold = True

                For i As Integer = 0 To pendingHeaders.Length - 1
                    ws.Cell(pendingStartRow + 1, i + 1).Value = pendingHeaders(i)
                    ws.Cell(pendingStartRow + 1, i + 1).Style.Font.Bold = True
                Next

                Dim pendingRow As Integer = pendingStartRow + 2
                Dim pendingTotal As Decimal = 0

                Using reader = Await ReadQueryAsync(pendingSql, params)
                    If reader IsNot Nothing Then
                        While Await reader.ReadAsync()

                            Dim itemName As String =
                                reader("item_name").ToString()

                            Dim itemPrice As Decimal =
                                Convert.ToDecimal(reader(Item.item_price))

                            Dim dropOffDate As DateTime =
                                Convert.ToDateTime(reader(Item.drop_off_date))

                            pendingTotal += itemPrice

                            ws.Cell(pendingRow, 1).Value = itemName
                            ws.Cell(pendingRow, 2).Value = itemPrice
                            ws.Cell(pendingRow, 3).Value = dropOffDate.ToString("MMMM dd, yyyy hh:mm tt")

                            pendingRow += 1

                        End While
                    End If
                End Using

                ws.Cell(pendingRow + 1, 1).Value = "TOTAL PENDING"
                ws.Cell(pendingRow + 1, 2).Value = pendingTotal
                ws.Cell(pendingRow + 1, 1).Style.Font.Bold = True
                ws.Cell(pendingRow + 1, 2).Style.Font.Bold = True

                ws.Columns().AdjustToContents()

                workbook.SaveAs(fullPath)

            End Using

            Process.Start(New ProcessStartInfo(fullPath) With {
                .UseShellExecute = True
            })

        Catch ex As Exception

            MessageBox.Show(
                ex.Message,
                "Revenue Report Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            )

        End Try

    End Function
End Class