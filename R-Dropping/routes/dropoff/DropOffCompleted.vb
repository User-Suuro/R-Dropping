Imports MySql.Data.MySqlClient

Public Class DropOffCompleted


    Inherits BasePanel
    Implements IRefreshable

    Private routeName As String = "Drop-off | Settled"
    Private _dgv As BaseDGV

    Public Sub RefreshPage() Implements IRefreshable.Refresh
        FetchDataForCompleted()
    End Sub

        Public Sub New()
        Me.Dock = DockStyle.Fill
        InitializeComponent()
        SetupEventHandlers()
        FetchDataForCompleted()
        root.RootInstance.SetRouteLabel(routeName)
    End Sub

        Private Sub InitializeComponent()
        _dgv = New BaseDGV()
        InitializeActionBtn()
        Me.Controls.Add(_dgv)
    End Sub


    Private _addBtn As BaseButton
    Private _refreshBtn As BaseButton
    Private _pendingBtn As BaseButton


    Private _deliverItem As BaseButton


    Private Sub InitializeActionBtn()

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


        _pendingBtn = New BaseButton With {
            .Text = "Pending",
            .Width = 105,
            .Height = 38,
            .Margin = Padding.Empty
        }


        _refreshBtn.SetPrimary()

        _pendingBtn.SetPrimary()


        _dgv.AddActionButton(_addBtn, ButtonVisibility.Always)
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


        AddHandler _addBtn.Click, Sub(sender, e)
                                      root.rootNav.GoToPage(New DropOffForm())
                                  End Sub


        AddHandler _refreshBtn.Click, Sub(sender, e)
                                          FetchDataForCompleted()

                                      End Sub

        AddHandler _pendingBtn.Click, Sub(sender, e)
                                          root.rootNav.GoToPage(New DropOffRoot())
                                      End Sub
    End Sub

    Private Async Sub FetchDataForCompleted()

            Dim loadingDlg As New BaseDialog()

            Await DialogTypes.ShowLoadingUntilAsync(
        loadingDlg,
        Form1.Instance,
        Async Function()
            Dim sql As String =
            $"SELECT 
                i.{Item.id},
                i.{Item.name},
                i.{Item.desc},
                i.{Item.drop_off_date},
                i.{Item.pickup_date},
                CONCAT(b.{Buyer.first_name}, ' ', b.{Buyer.last_name}) AS buyer_name,
                CONCAT(e.{Employee.first_name}, ' ', e.{Employee.last_name}) AS managed_by,
                s.{Seller.seller_name} AS seller_name,
                p.{Pricing.rate_label} AS pricing,
                st.{Storage.storage_name} AS storage
            FROM {Item.table_name} i
            LEFT JOIN {Buyer.table_name} b ON b.{Buyer.id} = i.{Item.buyer_id}
            LEFT JOIN {Employee.table_name} e ON e.{Employee.id} = i.{Item.managed_by}
            LEFT JOIN {Seller.table_name} s ON s.{Seller.id} = i.{Item.seller_id}
            LEFT JOIN {Pricing.table_name} p ON p.{Pricing.id} = i.{Item.pricing_id}
            LEFT JOIN {Stored.table_name} st_map ON st_map.{Stored.item_id} = i.{Item.id}
            LEFT JOIN {Storage.table_name} st ON st.{Storage.id} = st_map.{Stored.storage_id}
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
        {Item.name, 1},
        {"buyer_name", 2},
        {"seller_name", 3},
        {"pricing", 4},
        {"storage", 5},
        {Item.desc, 6}
    }

            For Each kvp In columnOrder
                If _dgv.DataGridView.Columns.Contains(kvp.Key) Then
                    _dgv.DataGridView.Columns(kvp.Key).DisplayIndex = kvp.Value
                End If
            Next

            Dim headerNames As New Dictionary(Of String, String) From {
        {Item.drop_off_date, "Drop-off Date"},
            {Item.name, "Item Name"},
            {"buyer_name", "Buyer"},
            {"seller_name", "Seller"},
            {"pricing", "Pricing"},
            {"storage", "Storage"},
            {Item.desc, "Description"}
        }

            For Each kvp In headerNames
                If _dgv.DataGridView.Columns.Contains(kvp.Key) Then
                    _dgv.DataGridView.Columns(kvp.Key).HeaderText = kvp.Value
                End If
            Next
        End Sub



End Class
