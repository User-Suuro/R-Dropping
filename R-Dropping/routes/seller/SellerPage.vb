Imports System.IO
Imports MySql.Data.MySqlClient

Public Class SellerPage
    Inherits BasePanel
    Implements IRefreshable

    Private routeName As String = "Sellers"

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
                                                Dim filterQuery = $"{Seller.seller_name} LIKE '%{searchText}%'"
                                                _dgv.FilterData(searchText, filterQuery)
                                                HandleCol()
                                            End Sub


        AddHandler _addBtn.Click, Sub(sender, e)
                                      root.rootNav.GoToPage(New SellerForm())
                                  End Sub

        AddHandler _updateBtn.Click, Sub(sender, e)
                                         Dim selectedRow = _dgv.GetSelectedRow()
                                         root.rootNav.GoToPage(New SellerForm(selectedRow.Cells(0).Value))
                                     End Sub

        AddHandler _deleteBtn.Click, Sub(sender, e)
                                         Dim selectedRow = _dgv.GetSelectedRow()
                                         DeleteData(selectedRow.Cells(0).Value)
                                     End Sub

        AddHandler _generateRevenueBtn.Click, Async Sub(sender, e)
                                                  Dim selectedRow = _dgv.GetSelectedRow()
                                                  Dim loadingDlg As New BaseDialog()

                                                  Await DialogTypes.ShowLoadingUntilAsync(
                                                  loadingDlg,
                                                  Form1.Instance,
                                                  Async Function()
                                                      Dim confirm_dlg = New BaseDialog()

                                                      DialogTypes.Apply(confirm_dlg,
                                                       DialogType.Confirmation,
                                                       "Confirmation",
                                                       "Are you sure you want to generate a revenue report for this buyer?")

                                                      If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
                                                          Await GenerateSellerRevenueReport(selectedRow.Cells(Item.seller_id).Value)
                                                      End If

                                                  End Function)
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
                Dim sql As String = $"SELECT * FROM {Seller.table_name}"

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
        If _dgv.DataGridView.Columns.Contains(Seller.id) Then
            _dgv.DataGridView.Columns(Seller.id).Visible = False
        End If
    End Sub

    Public Async Sub DeleteData(id As Integer)
        Dim confirm_dlg = New BaseDialog()

        DialogTypes.Apply(confirm_dlg,
                 DialogType.Confirmation,
                 "Confirmation",
                 "Are you sure you want to delete this Seller?")

        If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
            Dim sql As String = $"DELETE FROM {Seller.table_name} 
                                WHERE {Seller.id} = @{Seller.id}"


            Dim params As New Dictionary(Of String, Object) From {
                {$"@{Seller.id}", id}
            }

            Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)

            If affectedRows > 0 Then

                Dim info_dlg = New BaseDialog()

                DialogTypes.Apply(info_dlg,
                          DialogType.Info,
                          "Success",
                          "Seller data was deleted successfully")

                Await info_dlg.ShowBaseDialogAsync(Form1.Instance)

                FetchData()

            End If
        End If

        ' Handle error

    End Sub

    Private Async Function GenerateSellerRevenueReport(sellerId As Integer) As Task

        Try

            Dim relativePath As String =
                Config.BuildRelativePath(
                    "reports",
                    "seller_revenue",
                    "seller_revenue_report",
                    ".xlsx"
                )

            Dim fullPath As String =
                Path.Combine(Config.FindSolutionRoot(), relativePath)

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath))

            ' Items already picked up
            Dim settledSql As String =
                $"SELECT
                i.{Item.name}                                           AS item_name,
                i.{Item.item_price},
                CONCAT(b.{Buyer.first_name}, ' ', b.{Buyer.last_name}) AS buyer_name,
                p.{Pricing.rate_label}                                  AS rate_label,
                p.{Pricing.base_fee},
                p.{Pricing.daily_increment_fee},
                DATEDIFF(i.{Item.pickup_date}, i.{Item.drop_off_date})  AS days_stored,
                i.{Item.drop_off_date},
                i.{Item.pickup_date}
            FROM {Item.table_name} i
            JOIN {Buyer.table_name}   b ON b.{Buyer.id}   = i.{Item.buyer_id}
            JOIN {Pricing.table_name} p ON p.{Pricing.id} = i.{Item.pricing_id}
            WHERE i.{Item.pickup_date} IS NOT NULL
            AND i.{Item.seller_id} = @seller_id
            ORDER BY i.{Item.pickup_date} DESC"

            ' Items still in storage
            Dim pendingSql As String =
                $"SELECT
                i.{Item.name}                                           AS item_name,
                i.{Item.item_price},
                CONCAT(b.{Buyer.first_name}, ' ', b.{Buyer.last_name}) AS buyer_name,
                p.{Pricing.rate_label}                                  AS rate_label,
                p.{Pricing.base_fee},
                p.{Pricing.daily_increment_fee},
                DATEDIFF(CURDATE(), i.{Item.drop_off_date})             AS days_stored,
                i.{Item.drop_off_date}
            FROM {Item.table_name} i
            JOIN {Buyer.table_name}   b ON b.{Buyer.id}   = i.{Item.buyer_id}
            JOIN {Pricing.table_name} p ON p.{Pricing.id} = i.{Item.pricing_id}
            WHERE i.{Item.pickup_date} IS NULL
            AND i.{Item.seller_id} = @seller_id
            ORDER BY i.{Item.drop_off_date} DESC"

            Dim params As New Dictionary(Of String, Object) From {
                {"@seller_id", sellerId}
            }

            Using workbook As New ClosedXML.Excel.XLWorkbook()

                Dim ws = workbook.Worksheets.Add("Seller Revenue")

                ' ── SECTION 1 : Settled Revenue ───────────────────────────────────

                Dim settledHeaders As String() = {
                    "Item Name", "Item Price", "Buyer",
                    "Rate Plan", "Base Fee", "Daily Rate", "Days Stored", "Total Fee",
                    "Drop-off Date", "Pickup Date"
                }

                ws.Cell(1, 1).Value = "SETTLED REVENUE"
                ws.Cell(1, 1).Style.Font.Bold = True

                For i As Integer = 0 To settledHeaders.Length - 1
                    ws.Cell(2, i + 1).Value = settledHeaders(i)
                    ws.Cell(2, i + 1).Style.Font.Bold = True
                Next

                Dim row As Integer = 3
                Dim settledTotal As Decimal = 0

                Using reader = Await ReadQueryAsync(settledSql, params)
                    If reader IsNot Nothing Then
                        While Await reader.ReadAsync()

                            Dim baseFee As Decimal = Convert.ToDecimal(reader(Pricing.base_fee))
                            Dim dailyRate As Decimal = Convert.ToDecimal(reader(Pricing.daily_increment_fee))
                            Dim days As Integer = Convert.ToInt32(reader("days_stored"))
                            Dim totalFee As Decimal = baseFee + (days * dailyRate)

                            settledTotal += totalFee

                            ws.Cell(row, 1).Value = reader("item_name").ToString()
                            ws.Cell(row, 2).Value = Convert.ToDecimal(reader(Item.item_price))
                            ws.Cell(row, 3).Value = reader("buyer_name").ToString()
                            ws.Cell(row, 4).Value = reader("rate_label").ToString()
                            ws.Cell(row, 5).Value = baseFee
                            ws.Cell(row, 6).Value = dailyRate
                            ws.Cell(row, 7).Value = days
                            ws.Cell(row, 8).Value = totalFee
                            ws.Cell(row, 9).Value = Convert.ToDateTime(reader(Item.drop_off_date)).ToString("MMMM dd, yyyy hh:mm tt")
                            ws.Cell(row, 10).Value = Convert.ToDateTime(reader(Item.pickup_date)).ToString("MMMM dd, yyyy hh:mm tt")

                            row += 1
                        End While
                    End If
                End Using

                ws.Cell(row + 1, 1).Value = "TOTAL SETTLED"
                ws.Cell(row + 1, 8).Value = settledTotal
                ws.Cell(row + 1, 1).Style.Font.Bold = True
                ws.Cell(row + 1, 8).Style.Font.Bold = True

                ' ── SECTION 2 : Pending Fees ──────────────────────────────────────

                Dim pendingStartRow As Integer = row + 4

                Dim pendingHeaders As String() = {
                    "Item Name", "Item Price", "Buyer",
                    "Rate Plan", "Base Fee", "Daily Rate", "Days Stored", "Total Fee",
                    "Drop-off Date"
                }

                ws.Cell(pendingStartRow, 1).Value = "PENDING FEES"
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

                            Dim baseFee As Decimal = Convert.ToDecimal(reader(Pricing.base_fee))
                            Dim dailyRate As Decimal = Convert.ToDecimal(reader(Pricing.daily_increment_fee))
                            Dim days As Integer = Convert.ToInt32(reader("days_stored"))
                            Dim totalFee As Decimal = baseFee + (days * dailyRate)

                            pendingTotal += totalFee

                            ws.Cell(pendingRow, 1).Value = reader("item_name").ToString()
                            ws.Cell(pendingRow, 2).Value = Convert.ToDecimal(reader(Item.item_price))
                            ws.Cell(pendingRow, 3).Value = reader("buyer_name").ToString()
                            ws.Cell(pendingRow, 4).Value = reader("rate_label").ToString()
                            ws.Cell(pendingRow, 5).Value = baseFee
                            ws.Cell(pendingRow, 6).Value = dailyRate
                            ws.Cell(pendingRow, 7).Value = days
                            ws.Cell(pendingRow, 8).Value = totalFee
                            ws.Cell(pendingRow, 9).Value = Convert.ToDateTime(reader(Item.drop_off_date)).ToString("MMMM dd, yyyy hh:mm tt")

                            pendingRow += 1
                        End While
                    End If
                End Using

                ws.Cell(pendingRow + 1, 1).Value = "TOTAL PENDING"
                ws.Cell(pendingRow + 1, 8).Value = pendingTotal
                ws.Cell(pendingRow + 1, 1).Style.Font.Bold = True
                ws.Cell(pendingRow + 1, 8).Style.Font.Bold = True

                ws.Columns().AdjustToContents()
                workbook.SaveAs(fullPath)

            End Using

            Process.Start(New ProcessStartInfo(fullPath) With {
                .UseShellExecute = True
            })

        Catch ex As Exception
            MessageBox.Show(ex.Message, "Revenue Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

    End Function
End Class
