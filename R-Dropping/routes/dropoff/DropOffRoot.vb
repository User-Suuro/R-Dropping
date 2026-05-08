Imports System.IO
Imports MySql.Data.MySqlClient

Public Class DropOffRoot
    Inherits BasePanel

    Private routeName As String = "Drop-off"

    Private _panelNavTop As Panel
    Private _dropOffRootPanel As Panel


    Private _onDeliveryBtn As BaseButton
    Private _completedBtn As BaseButton
    Private _pendingBtn As BaseButton
    Private _revenueReportBtn As BaseButton

    Private _dropOffPage As DropOffPage

    Public Shared DropOffRootInstance As DropOffRoot
    Public Shared _dropOffRootNav As NavigationManager


    Public Sub New()
        DropOffRootInstance = Me
        Me.Dock = DockStyle.Fill
        root.RootInstance.SetRouteLabel(routeName)
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        _panelNavTop = New Panel()

        With _panelNavTop
            .Dock = DockStyle.Top
            .Height = 44
            .Padding = New Padding(4)
        End With

        _dropOffRootPanel = New Panel()


        With _dropOffRootPanel
            .Dock = DockStyle.Fill
        End With

        _dropOffRootNav = New NavigationManager(_dropOffRootPanel)

        _completedBtn = New BaseButton With {
            .Text = "Settled",
            .Height = 38,
            .Margin = Padding.Empty,
            .BorderRadius = 0
        }

        _completedBtn.SetPrimary()

        _pendingBtn = New BaseButton With {
            .Text = "Pending",
            .Height = 38,
            .Margin = Padding.Empty,
            .BorderRadius = 0
        }

        _pendingBtn.SetPrimary()

        _onDeliveryBtn = New BaseButton With {
            .Text = "On Delivery",
            .Height = 38,
            .Margin = Padding.Empty,
            .BorderRadius = 0
        }

        _onDeliveryBtn.SetPrimary()

        _revenueReportBtn = New BaseButton With {
            .Text = "Revenue Report",
            .Height = 38,
            .Margin = Padding.Empty,
            .BorderRadius = 0
        }

        _revenueReportBtn.SetPrimary()

        Dim navTable As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 4,
            .RowCount = 1
        }

        navTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))
        navTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))
        navTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))
        navTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))

        navTable.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

        _completedBtn.Dock = DockStyle.Fill
        _onDeliveryBtn.Dock = DockStyle.Fill
        _pendingBtn.Dock = DockStyle.Fill

        navTable.Controls.Add(_pendingBtn, 0, 0)
        navTable.Controls.Add(_onDeliveryBtn, 1, 0)
        navTable.Controls.Add(_completedBtn, 2, 0)
        navTable.Controls.Add(_revenueReportBtn, 3, 0)

        _panelNavTop.Controls.Add(navTable)


        Me.Controls.Add(_dropOffRootPanel)
        Me.Controls.Add(_panelNavTop)

        _dropOffRootNav.GoToPage(New DropOffPage())

        AddHandler _completedBtn.Click, Sub(sender, e)
                                            _dropOffRootNav.GoToPage(New DropOffCompleted())
                                            _completedBtn.SetSecondary()
                                            _pendingBtn.SetPrimary()
                                            _onDeliveryBtn.SetPrimary()
                                            _revenueReportBtn.SetPrimary()
                                        End Sub
        AddHandler _pendingBtn.Click, Sub(sender, e)
                                          _dropOffRootNav.GoToPage(New DropOffPage())
                                          _completedBtn.SetPrimary()
                                          _pendingBtn.SetSecondary()
                                          _onDeliveryBtn.SetPrimary()
                                          _revenueReportBtn.SetPrimary()
                                      End Sub

        AddHandler _onDeliveryBtn.Click, Sub(sender, e)
                                             _dropOffRootNav.GoToPage(New DropOffOnDelivery())
                                             _completedBtn.SetPrimary()
                                             _pendingBtn.SetPrimary()
                                             _onDeliveryBtn.SetSecondary()
                                             _revenueReportBtn.SetPrimary()
                                         End Sub
        AddHandler _revenueReportBtn.Click, Async Sub(sender, e)

                                                Dim loadingDlg As New BaseDialog()

                                                Await DialogTypes.ShowLoadingUntilAsync(
                                                loadingDlg,
                                                Form1.Instance,
                                                Async Function()
                                                    Dim confirm_dlg = New BaseDialog()

                                                    DialogTypes.Apply(confirm_dlg,
                                                     DialogType.Confirmation,
                                                     "Confirmation",
                                                     "Are you sure you want to generate a revenue report?")

                                                    If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
                                                        Await GenerateDropOffRevenueReport()
                                                    End If

                                                End Function)


                                            End Sub
    End Sub

    Private Async Function GenerateDropOffRevenueReport() As Task

        Try

            Dim relativePath As String =
                Config.BuildRelativePath(
                    "reports",
                    "dropoff_revenue",
                    "dropoff_revenue_report",
                    ".xlsx"
                )

            Dim fullPath As String =
                Path.Combine(Config.FindSolutionRoot(), relativePath)

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath))

            ' Items already picked up
            Dim settledSql As String =
                $"SELECT
                i.{Item.name}                                           AS item_name,
                CONCAT(b.{Buyer.first_name}, ' ', b.{Buyer.last_name}) AS buyer_name,
                s.{Seller.seller_name}                                  AS seller_name,
                p.{Pricing.rate_label}                                  AS rate_label,
                p.{Pricing.base_fee},
                p.{Pricing.daily_increment_fee},
                DATEDIFF(i.{Item.pickup_date}, i.{Item.drop_off_date})  AS days_stored,
                i.{Item.drop_off_date},
                i.{Item.pickup_date}
            FROM {Item.table_name} i
            JOIN {Buyer.table_name}   b ON b.{Buyer.id}   = i.{Item.buyer_id}
            JOIN {Seller.table_name}  s ON s.{Seller.id}  = i.{Item.seller_id}
            JOIN {Pricing.table_name} p ON p.{Pricing.id} = i.{Item.pricing_id}
            WHERE i.{Item.pickup_date} IS NOT NULL
            ORDER BY i.{Item.pickup_date} DESC"

            ' Items still in storage
            Dim pendingSql As String =
                $"SELECT
                i.{Item.name}                                           AS item_name,
                CONCAT(b.{Buyer.first_name}, ' ', b.{Buyer.last_name}) AS buyer_name,
                s.{Seller.seller_name}                                  AS seller_name,
                p.{Pricing.rate_label}                                  AS rate_label,
                p.{Pricing.base_fee},
                p.{Pricing.daily_increment_fee},
                DATEDIFF(CURDATE(), i.{Item.drop_off_date})             AS days_stored,
                i.{Item.drop_off_date}
            FROM {Item.table_name} i
            JOIN {Buyer.table_name}   b ON b.{Buyer.id}   = i.{Item.buyer_id}
            JOIN {Seller.table_name}  s ON s.{Seller.id}  = i.{Item.seller_id}
            JOIN {Pricing.table_name} p ON p.{Pricing.id} = i.{Item.pricing_id}
            WHERE i.{Item.pickup_date} IS NULL
            ORDER BY i.{Item.drop_off_date} DESC"

            Using workbook As New ClosedXML.Excel.XLWorkbook()

                Dim ws = workbook.Worksheets.Add("Drop-off Revenue")

                Dim headers As String() = {
                    "Item Name", "Buyer", "Seller",
                    "Rate Plan", "Base Fee", "Daily Rate", "Days Stored", "Total Fee"
                }

                ' ── SECTION 1 : Settled Revenue ───────────────────────────────────

                ws.Cell(1, 1).Value = "SETTLED REVENUE"
                ws.Cell(1, 1).Style.Font.Bold = True

                For i As Integer = 0 To headers.Length - 1
                    ws.Cell(2, i + 1).Value = headers(i)
                    ws.Cell(2, i + 1).Style.Font.Bold = True
                Next

                Dim row As Integer = 3
                Dim settledTotal As Decimal = 0

                Using reader = Await ReadQueryAsync(settledSql)
                    If reader IsNot Nothing Then
                        While Await reader.ReadAsync()

                            Dim baseFee As Decimal = Convert.ToDecimal(reader(Pricing.base_fee))
                            Dim dailyRate As Decimal = Convert.ToDecimal(reader(Pricing.daily_increment_fee))
                            Dim days As Integer = Convert.ToInt32(reader("days_stored"))
                            Dim totalFee As Decimal = baseFee + (days * dailyRate)

                            settledTotal += totalFee

                            ws.Cell(row, 1).Value = reader("item_name").ToString()
                            ws.Cell(row, 2).Value = reader("buyer_name").ToString()
                            ws.Cell(row, 3).Value = reader("seller_name").ToString()
                            ws.Cell(row, 4).Value = reader("rate_label").ToString()
                            ws.Cell(row, 5).Value = baseFee
                            ws.Cell(row, 6).Value = dailyRate
                            ws.Cell(row, 7).Value = days
                            ws.Cell(row, 8).Value = totalFee

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

                ws.Cell(pendingStartRow, 1).Value = "PENDING FEES"
                ws.Cell(pendingStartRow, 1).Style.Font.Bold = True

                For i As Integer = 0 To headers.Length - 1
                    ws.Cell(pendingStartRow + 1, i + 1).Value = headers(i)
                    ws.Cell(pendingStartRow + 1, i + 1).Style.Font.Bold = True
                Next

                Dim pendingRow As Integer = pendingStartRow + 2
                Dim pendingTotal As Decimal = 0

                Using reader = Await ReadQueryAsync(pendingSql)
                    If reader IsNot Nothing Then
                        While Await reader.ReadAsync()

                            Dim baseFee As Decimal = Convert.ToDecimal(reader(Pricing.base_fee))
                            Dim dailyRate As Decimal = Convert.ToDecimal(reader(Pricing.daily_increment_fee))
                            Dim days As Integer = Convert.ToInt32(reader("days_stored"))
                            Dim totalFee As Decimal = baseFee + (days * dailyRate)

                            pendingTotal += totalFee

                            ws.Cell(pendingRow, 1).Value = reader("item_name").ToString()
                            ws.Cell(pendingRow, 2).Value = reader("buyer_name").ToString()
                            ws.Cell(pendingRow, 3).Value = reader("seller_name").ToString()
                            ws.Cell(pendingRow, 4).Value = reader("rate_label").ToString()
                            ws.Cell(pendingRow, 5).Value = baseFee
                            ws.Cell(pendingRow, 6).Value = dailyRate
                            ws.Cell(pendingRow, 7).Value = days
                            ws.Cell(pendingRow, 8).Value = totalFee

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
