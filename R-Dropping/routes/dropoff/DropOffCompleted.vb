Imports MySql.Data.MySqlClient

Public Class DropOffCompleted

    Inherits BasePanel
    Private routeName As String = "Drop-off | Settled"
    Private _dgv As BaseDGV
    Private _initialized As Boolean = False
    Private _isFetching As Boolean = False

    Public Sub New()
        Me.Dock = DockStyle.Fill
        InitializeComponent()
        SetupEventHandlers()
        FetchDataForCompleted()
        root.RootInstance.SetRouteLabel(routeName)
        _initialized = True
        AddHandler _dgv.AfterPageApplied, Sub(s, e) HandleCol()
    End Sub

    Protected Overrides Sub OnVisibleChanged(e As EventArgs)
        MyBase.OnVisibleChanged(e)
        If Me.Visible AndAlso _initialized AndAlso _isFetching Then
            FetchDataForCompleted()
        End If
    End Sub

    Private Sub InitializeComponent()
        _dgv = New BaseDGV()
        InitializeActionBtn()
        Me.Controls.Add(_dgv)
    End Sub


    Private _refreshBtn As BaseButton
    Private _pendingBtn As BaseButton


    Private Sub InitializeActionBtn()

        _refreshBtn = New BaseButton With {
            .Text = "Refresh",
            .Width = 105,
            .Height = 38,
            .Margin = Padding.Empty
        }


        _pendingBtn = New BaseButton With {
            .Text = "Pending",
            .Width = 105,
            .Height = 38,
            .Margin = Padding.Empty
        }


        _refreshBtn.SetPrimary()

        _pendingBtn.SetPrimary()


        _dgv.AddActionButton(_refreshBtn, ButtonVisibility.Always)

    End Sub



    Private Sub SetupEventHandlers()


        AddHandler _dgv.SearchButton.Click, Sub(sender, e)
                                                Dim searchText = _dgv.GetSearchText()

                                                Dim filterQuery As String


                                                filterQuery = $"{Item.name} LIKE '%{searchText}%' " &
                                                          $"OR seller_name LIKE '%{searchText}%' " &
                                                          $"OR buyer_name LIKE '%{searchText}%' "


                                                _dgv.FilterData(searchText, filterQuery)
                                                HandleCol()
                                            End Sub




        AddHandler _refreshBtn.Click, Sub(sender, e)
                                          FetchDataForCompleted()

                                      End Sub

        AddHandler _pendingBtn.Click, Sub(sender, e)
                                          root.rootNav.GoToPage(New DropOffRoot())
                                      End Sub
    End Sub

    Private Async Sub FetchDataForCompleted()
        If _isFetching Then Exit Sub
        _isFetching = True
        Try
            Dim loadingDlg As New BaseDialog()

            Await DialogTypes.ShowLoadingUntilAsync(
            loadingDlg,
            Form1.Instance,
            Async Function()
                Dim sql As String =
                $"SELECT 
                    i.{Item.id},
                    i.{Item.name},
                    DATE_FORMAT(i.{Item.drop_off_date}, '%m-%d-%Y %h:%i %p') AS {Item.drop_off_date},
                    DATE_FORMAT(i.{Item.pickup_date}, '%m-%d-%Y %h:%i %p') AS {Item.pickup_date},
                    DATE_FORMAT(d.{Delivery.date_delivered}, '%m-%d-%Y %h:%i %p') AS {Delivery.date_delivered},
                    CONCAT(b.{Buyer.first_name}, ' ', b.{Buyer.last_name}) AS buyer_name,
                    CONCAT(e.{Employee.first_name}, ' ', e.{Employee.last_name}) AS managed_by,
                    s.{Seller.seller_name} AS seller_name,
                    p.{Pricing.base_fee},
                    p.{Pricing.daily_increment_fee},
                    (p.{Pricing.base_fee} + (DATEDIFF(i.{Item.pickup_date}, i.{Item.drop_off_date}) * p.{Pricing.daily_increment_fee})) AS total_amount
                FROM {Item.table_name} i
                LEFT JOIN {Buyer.table_name} b ON b.{Buyer.id} = i.{Item.buyer_id}
                LEFT JOIN {Employee.table_name} e ON e.{Employee.id} = i.{Item.managed_by}
                LEFT JOIN {Seller.table_name} s ON s.{Seller.id} = i.{Item.seller_id}
                LEFT JOIN {Pricing.table_name} p ON p.{Pricing.id} = i.{Item.pricing_id}
                LEFT JOIN {Stored.table_name} st_map ON st_map.{Stored.item_id} = i.{Item.id}
                LEFT JOIN {Delivery.table_name} d ON d.{Delivery.item_id} = i.{Item.id}
                WHERE i.{Item.pickup_date} IS NOT NULL
                  AND (d.{Delivery.item_id} IS NULL OR d.{Delivery.date_delivered} IS NOT NULL)"
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
        Finally
            _isFetching = False
        End Try

    End Sub

    Private Sub HandleCol()
        Dim hiddenCols = {Item.id}
        For Each col In hiddenCols
            If _dgv.DataGridView.Columns.Contains(col) Then
                _dgv.DataGridView.Columns(col).Visible = False
            End If
        Next

        Dim columnOrder As New Dictionary(Of String, Integer) From {
        {Item.drop_off_date, 0},
        {Item.pickup_date, 1},
        {Delivery.date_delivered, 2},
        {Item.name, 3},
        {"buyer_name", 4},
        {"seller_name", 5},
        {Pricing.base_fee, 6},
        {Pricing.daily_increment_fee, 7},
        {"total_amount", 8}
    }
        For Each kvp In columnOrder
            If _dgv.DataGridView.Columns.Contains(kvp.Key) Then
                _dgv.DataGridView.Columns(kvp.Key).DisplayIndex = kvp.Value
            End If
        Next

        Dim headerNames As New Dictionary(Of String, String) From {
        {Item.drop_off_date, "Drop-off Date"},
        {Item.pickup_date, "Pickup Date"},
        {Delivery.date_delivered, "Delivered Date"},
        {Item.name, "Item Name"},
        {"buyer_name", "Buyer"},
        {"seller_name", "Seller"},
        {Pricing.base_fee, "Base Fee"},
        {Pricing.daily_increment_fee, "Daily Fee"},
        {"total_amount", "Total Amount"}
    }
        For Each kvp In headerNames
            If _dgv.DataGridView.Columns.Contains(kvp.Key) Then
                _dgv.DataGridView.Columns(kvp.Key).HeaderText = kvp.Value
            End If
        Next
    End Sub



End Class
