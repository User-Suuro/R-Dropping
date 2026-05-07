Imports MySql.Data.MySqlClient

Public Class DropOffOnDelivery

    Inherits BasePanel
    Private routeName As String = "Drop-off | Delivery"
    Private _dgv As BaseDGV
    Private _initialized As Boolean = False
    Private _isFetching As Boolean = False

    Public Sub New()
        Me.Dock = DockStyle.Fill
        InitializeComponent()
        SetupEventHandlers()
        FetchDataForOnDeliver()
        root.RootInstance.SetRouteLabel(routeName)
        _initialized = True
        AddHandler _dgv.AfterPageApplied, Sub(s, e) HandleCol()
    End Sub

    Protected Overrides Sub OnVisibleChanged(e As EventArgs)
        MyBase.OnVisibleChanged(e)
        If Me.Visible AndAlso _initialized AndAlso _isFetching Then
            FetchDataForOnDeliver()
        End If
    End Sub

    Private Sub InitializeComponent()
        _dgv = New BaseDGV()
        InitializeActionBtn()
        Me.Controls.Add(_dgv)
    End Sub


    Private _refreshBtn As BaseButton
    Private _pendingBtn As BaseButton
    Private _deliverItem As BaseButton
    Private _deliverBtn As BaseButton


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
        _deliverBtn = New BaseButton With {
            .Text = "Mark as Delivered",
            .Width = 140,
            .Height = 38,
            .Margin = Padding.Empty
        }

        _deliverBtn.SetPrimary()

        _refreshBtn.SetPrimary()

        _pendingBtn.SetPrimary()

        _dgv.AddActionButton(_deliverBtn, ButtonVisibility.OnSelection)
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


        AddHandler _deliverBtn.Click, Sub(sender, e)
                                          Dim selectedRow = _dgv.GetSelectedRow()
                                          MarkAsDelivered(selectedRow.Cells($"{Item.id}").Value)
                                      End Sub

        AddHandler _refreshBtn.Click, Sub(sender, e)
                                          FetchDataForOnDeliver()
                                      End Sub

        AddHandler _pendingBtn.Click, Sub(sender, e)
                                          root.rootNav.GoToPage(New DropOffRoot())
                                      End Sub
    End Sub

    Private Async Sub FetchDataForOnDeliver()
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

                s.{Seller.seller_name} AS seller_name,
                CONCAT(b.{Buyer.first_name}, ' ', b.{Buyer.last_name}) AS buyer_name,
                CONCAT(c.{Courier.first_name}, ' ', c.{Courier.last_name}) AS courier_name,

                d.{Delivery.shipping_fee},
                DATE_FORMAT(i.{Item.drop_off_date}, '%m-%d-%Y %h:%i %p') AS drop_off_datetime,
                DATE_FORMAT(i.{Item.pickup_date}, '%m-%d-%Y %h:%i %p') AS pickup_datetime

            FROM {Delivery.table_name} d
            INNER JOIN {Item.table_name} i ON i.{Item.id} = d.{Delivery.item_id}
            LEFT JOIN {Buyer.table_name} b ON b.{Buyer.id} = i.{Item.buyer_id}
            LEFT JOIN {Seller.table_name} s ON s.{Seller.id} = i.{Item.seller_id}
            LEFT JOIN {Courier.table_name} c ON c.{Courier.id} = d.{Delivery.courier_id}
            WHERE d.{Delivery.date_delivered} IS NULL"

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

    Public Async Sub MarkAsDelivered(id As Integer)
        Dim confirm_dlg = New BaseDialog()

        DialogTypes.Apply(confirm_dlg,
             DialogType.Confirmation,
             "Confirmation",
             "Are you sure you want to mark this item as delivered?")

        If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
            Dim sql As String = $"UPDATE {Delivery.table_name} 
                            SET {Delivery.date_delivered} = NOW()
                            WHERE {Delivery.item_id} = @{Delivery.item_id}"

            Dim params As New Dictionary(Of String, Object) From {
            {$"@{Delivery.item_id}", id}
        }

            Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)

            If affectedRows > 0 Then
                Dim info_dlg = New BaseDialog()

                DialogTypes.Apply(info_dlg,
                      DialogType.Info,
                      "Success",
                      "Item has been marked as delivered.")

                Await info_dlg.ShowBaseDialogAsync(Form1.Instance)

                FetchDataForOnDeliver()
            End If
        End If

    End Sub

    Private Sub HandleCol()
        Dim hiddenCols = {Item.id}
        For Each col In hiddenCols
            If _dgv.DataGridView.Columns.Contains(col) Then
                _dgv.DataGridView.Columns(col).Visible = False
            End If
        Next

        Dim columnOrder As New Dictionary(Of String, Integer) From {
            {"drop_off_datetime", 0},
            {"pickup_datetime", 1},
            {Item.name, 2},
            {"buyer_name", 3},
            {"seller_name", 4},
            {"courier_name", 5},
            {Delivery.shipping_fee, 6}
        }
        For Each kvp In columnOrder
            If _dgv.DataGridView.Columns.Contains(kvp.Key) Then
                _dgv.DataGridView.Columns(kvp.Key).DisplayIndex = kvp.Value
            End If
        Next

        Dim headerNames As New Dictionary(Of String, String) From {
            {"drop_off_datetime", "Drop-off"},
            {"pickup_datetime", "Picked Up"},
            {Item.name, "Item Name"},
            {"seller_name", "Seller"},
            {"buyer_name", "Buyer"},
            {"courier_name", "Courier"},
            {Delivery.shipping_fee, "Shipping Fee"}
        }
        For Each kvp In headerNames
            If _dgv.DataGridView.Columns.Contains(kvp.Key) Then
                _dgv.DataGridView.Columns(kvp.Key).HeaderText = kvp.Value
            End If
        Next
    End Sub

End Class
