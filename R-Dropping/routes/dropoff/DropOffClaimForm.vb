Public Class DropOffClaimForm
    Inherits BasePanel

    Private _tableFormat As TableLayoutPanel
    Private _subContainerLeft As PrimaryFlowLayoutPanel

    ' Info display fields
    Private _itemNamePlaceholder As BaseInputPanel
    Private _buyerNamePlaceholder As BaseInputPanel
    Private _sellerNamePlaceholder As BaseInputPanel

    ' Pricing display fields
    Private _basePricingPlaceholder As BaseInputPanel
    Private _dailyPricingPlaceholder As BaseInputPanel
    Private _daysPlaceholder As BaseInputPanel
    Private _totalFeePlaceholder As BaseInputPanel

    ' Image
    Private _imagePanel As BaseImagePanel

    ' Courier
    Private _courierCmb As BaseComboBox
    Private _courierField As ValidationPanel
    Private _shippingFeePanel As BaseNumericPanel
    Private _courierRow As TableLayoutPanel

    Private _addButton As BaseButton
    Private _cancelButton As BaseButton
    Private _buttonTable As TableLayoutPanel


    Private _id As Integer?
    Private _baseFee As Decimal = 0
    Private _dailyFee As Decimal = 0

    Public Sub New(Optional id As Integer? = Nothing)
        Me.Dock = DockStyle.Fill
        _id = id
        InitializeComponent()
        _tableFormat.Visible = False
        AddHandler Me.Resize, AddressOf CenterTableFormat
        AddHandler _tableFormat.SizeChanged, AddressOf CenterTableFormat
        CenterTableFormat(Nothing, EventArgs.Empty)
        LoadAsync()
    End Sub

    Public Sub InitializeComponent()

        ' ── Outer table (mirrors DropOffForm pattern) ──────────────────────────
        _tableFormat = New TableLayoutPanel() With {
            .ColumnCount = 2,
            .RowCount = 3,
            .Padding = New Padding(16),
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Height = 460,
            .Width = 520,
            .BorderStyle = BorderStyle.FixedSingle
        }

        With _tableFormat.ColumnStyles
            .Add(New ColumnStyle(SizeType.Percent, 55))
            .Add(New ColumnStyle(SizeType.Percent, 45))
        End With

        With _tableFormat.RowStyles
            .Add(New RowStyle(SizeType.Percent, 75))
            .Add(New RowStyle(SizeType.AutoSize))
            .Add(New RowStyle(SizeType.Absolute, 48))
        End With

        Dim leftWrapper As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White
        }

        _subContainerLeft = New PrimaryFlowLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .WrapContents = False,
            .AutoScroll = True,
            .BackColor = Color.White,
            .FlowDirection = FlowDirection.TopDown
        }


        leftWrapper.Controls.Add(_subContainerLeft)

        _itemNamePlaceholder = New BaseInputPanel() With {
            .LabelText = "Item Name",
            .Enabled = False
        }

        _buyerNamePlaceholder = New BaseInputPanel() With {
            .LabelText = "Buyer",
            .Enabled = False
        }

        _sellerNamePlaceholder = New BaseInputPanel() With {
            .LabelText = "Seller",
            .Enabled = False
        }

        ' Pricing table (Base Fee | Daily Fee)
        Dim pricingTable As New TableLayoutPanel With {
            .ColumnCount = 2,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Width = _subContainerLeft.Width + 8,
            .Height = _buyerNamePlaceholder.Height
        }
        pricingTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        pricingTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

        _basePricingPlaceholder = New BaseInputPanel() With {
            .LabelText = "Base Fee",
            .Enabled = False,
            .Dock = DockStyle.Top
        }

        _dailyPricingPlaceholder = New BaseInputPanel() With {
            .LabelText = "Daily Fee",
            .Enabled = False,
            .Dock = DockStyle.Top
        }

        pricingTable.Controls.Add(_basePricingPlaceholder, 0, 0)
        pricingTable.Controls.Add(_dailyPricingPlaceholder, 1, 0)

        ' Calc table (Days Stored | Total Fee)
        Dim calcTable As New TableLayoutPanel With {
            .ColumnCount = 2,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Width = _subContainerLeft.Width + 8,
            .Height = _dailyPricingPlaceholder.Height
        }
        calcTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        calcTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

        _daysPlaceholder = New BaseInputPanel() With {
            .LabelText = "Days Stored",
            .Enabled = False,
            .Dock = DockStyle.Top
        }

        _totalFeePlaceholder = New BaseInputPanel() With {
            .LabelText = "Total Fee",
            .Enabled = False,
            .Dock = DockStyle.Top
        }

        calcTable.Controls.Add(_daysPlaceholder, 0, 0)
        calcTable.Controls.Add(_totalFeePlaceholder, 1, 0)

        With _subContainerLeft.Controls
            .Add(_itemNamePlaceholder)
            .Add(_buyerNamePlaceholder)
            .Add(_sellerNamePlaceholder)
            .Add(pricingTable)
            .Add(calcTable)
        End With

        _imagePanel = New BaseImagePanel() With {
            .PlaceholderText = "",
            .LabelText = "Item Image",
            .Dock = DockStyle.Fill,
            .Enabled = False
        }


        _courierRow = New TableLayoutPanel With {
            .ColumnCount = 2,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Dock = DockStyle.Fill,
            .AutoSize = True
        }
        _courierRow.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 55))
        _courierRow.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 45))


        _courierCmb = New BaseComboBox("Courier (Optional)") With {
            .Placeholder = "Select Courier",
            .Dock = DockStyle.Top
        }

        _courierCmb.SetClearable()

        _courierField = New ValidationPanel(_courierCmb)

        _shippingFeePanel = New BaseNumericPanel() With {
            .LabelText = "Shipping Fee",
            .Dock = DockStyle.Fill,
            .Visible = False
        }

        _shippingFeePanel.SetPricingMode()

        _courierRow.Controls.Add(_courierField, 0, 0)
        _courierRow.Controls.Add(_shippingFeePanel, 1, 0)


        ' ── Button row (spans both columns) ───────────────────────────────────
        _buttonTable = New TableLayoutPanel With {
            .ColumnCount = 2,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Dock = DockStyle.Fill,
            .Height = 40
        }
        _buttonTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        _buttonTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

        _addButton = New BaseButton() With {.Text = "Claim", .Dock = DockStyle.Top}
        _addButton.SetPrimary()

        _cancelButton = New BaseButton() With {.Text = "Cancel", .Dock = DockStyle.Top}
        _cancelButton.SetDanger()

        _buttonTable.Controls.Add(_cancelButton, 0, 0)
        _buttonTable.Controls.Add(_addButton, 1, 0)


        With _tableFormat.Controls
            .Add(leftWrapper, 0, 0)
            .Add(_imagePanel, 1, 0)
            .Add(_courierRow, 0, 1)
            .Add(_buttonTable, 0, 2)
        End With


        _tableFormat.SetColumnSpan(_courierRow, 2)
        _tableFormat.SetColumnSpan(_buttonTable, 2)

        Me.Controls.Add(_tableFormat)

        ' ── Bind events ───────────────────────────────────────────────────────
        AddHandler _addButton.Click, AddressOf QueryClaim
        AddHandler _cancelButton.Click, AddressOf CancelAdd
        AddHandler _courierCmb.SelectedValueChanged, AddressOf CourierChanged
    End Sub


    Private Sub CourierChanged(sender As Object, e As EventArgs)
        Dim hasSelection = Not String.IsNullOrWhiteSpace(_courierCmb.SelectedValue)
        _shippingFeePanel.Visible = hasSelection
        If Not hasSelection Then
            _shippingFeePanel.SetValue(0)
        End If
    End Sub
    Private Sub ClearCourier()
        _courierCmb.ClearSelection()
        _shippingFeePanel.Visible = False
        _shippingFeePanel.SetValue(0)
    End Sub

    ' ── Data loading ──────────────────────────────────────────────────────────

    Private Async Sub LoadAsync()
        If Not _id.HasValue Then Exit Sub
        Try
            Dim loadingDlg As New BaseDialog()
            Await DialogTypes.ShowLoadingUntilAsync(
                loadingDlg,
                Form1.Instance,
                Async Function()
                    Await LoadCouriers()
                    Await FetchItemDetails(_id.Value)
                End Function
            )
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try
    End Sub

    Private Async Function LoadCouriers() As Task
        Dim list As New List(Of ComboItem)

        Dim sql As String =
        $"SELECT {Courier.id}, CONCAT({Courier.first_name}, ' ', {Courier.last_name}) AS name
      FROM {Courier.table_name}"

        Using reader = Await ReadQueryAsync(sql)
            If reader IsNot Nothing Then
                While Await reader.ReadAsync()
                    list.Add(New ComboItem(
                    reader(Courier.id).ToString(),
                    reader("name").ToString()
                ))
                End While
            End If
        End Using

        _courierCmb.ComboItems = list
    End Function

    Private Async Function FetchItemDetails(id As Integer) As Task

        Dim sql As String =
        $"SELECT
            i.{Item.name},
            CONCAT(b.{Buyer.first_name}, ' ', b.{Buyer.last_name}) AS buyer_name,
            s.{Seller.seller_name},
            p.{Pricing.base_fee},
            p.{Pricing.daily_increment_fee},
            i.{Item.drop_off_date},
            i.{Item.img_path}
        FROM {Item.table_name} i
        LEFT JOIN {Buyer.table_name} b
            ON b.{Buyer.id} = i.{Item.buyer_id}
        LEFT JOIN {Seller.table_name} s
            ON s.{Seller.id} = i.{Item.seller_id}
        LEFT JOIN {Pricing.table_name} p
            ON p.{Pricing.id} = i.{Item.pricing_id}
        WHERE i.{Item.id} = @id"

        Dim params As New Dictionary(Of String, Object) From {{"@id", id}}

        Try
            Using reader = Await ReadQueryAsync(sql, params)
                If reader IsNot Nothing AndAlso Await reader.ReadAsync() Then

                    _itemNamePlaceholder.SetValue(reader(Item.name).ToString())
                    _buyerNamePlaceholder.SetValue(reader("buyer_name").ToString())
                    _sellerNamePlaceholder.SetValue(reader(Seller.seller_name).ToString())

                    _baseFee = Convert.ToDecimal(reader(Pricing.base_fee))
                    _dailyFee = Convert.ToDecimal(reader(Pricing.daily_increment_fee))

                    _basePricingPlaceholder.SetValue(_baseFee.ToString("F2"))
                    _dailyPricingPlaceholder.SetValue(_dailyFee.ToString("F2"))

                    If reader(Item.drop_off_date) IsNot DBNull.Value Then
                        Dim dropOffDate = Convert.ToDateTime(reader(Item.drop_off_date))
                        Dim days = Math.Max(1, (DateTime.Now - dropOffDate).Days)
                        Dim total = _baseFee + (_dailyFee * days)

                        _daysPlaceholder.SetValue(days.ToString())
                        _totalFeePlaceholder.SetValue(total.ToString("F2"))
                    Else
                        _daysPlaceholder.SetValue("N/A")
                        _totalFeePlaceholder.SetValue("N/A")
                    End If

                    Dim imgPath = reader(Item.img_path)
                    If imgPath IsNot DBNull.Value Then
                        _imagePanel.LoadImage(imgPath.ToString())
                    End If

                End If
            End Using
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try

    End Function

    ' ── Claim query ───────────────────────────────────────────────────────────

    Private Async Sub QueryClaim()
        If Not _id.HasValue Then Exit Sub

        Dim confirmDlg As New BaseDialog()
        DialogTypes.Apply(confirmDlg,
                          DialogType.Confirmation,
                          "Confirm Claim",
                          "Are you sure you want to claim this item? This will mark it as picked up.")

        If Await confirmDlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
            Dim loadingDlg As New BaseDialog()

            Await DialogTypes.ShowLoadingUntilAsync(
                loadingDlg,
                Form1.Instance,
                Async Function()
                    Dim success As Boolean = Await ClaimQuery()

                    If success Then
                        Dim infoDlg As New BaseDialog()
                        DialogTypes.Apply(infoDlg,
                                          DialogType.Info,
                                          "Success",
                                          "Item has been claimed successfully.")

                        If Await infoDlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
                            root.rootNav.GoBackPage()
                        End If
                    Else
                        Dim errorDlg As New BaseDialog()
                        DialogTypes.Apply(errorDlg,
                                          DialogType.Error,
                                          "Error",
                                          "Something went wrong while claiming the item.")
                        errorDlg.ShowDialog()
                    End If
                End Function
            )
        End If
    End Sub

    Private Async Function ClaimQuery() As Task(Of Boolean)

        Dim claimTime = DateTime.Now

        Dim sql As String =
        $"UPDATE {Item.table_name} SET
        {Item.pickup_date} = @pickup_date
        WHERE {Item.id} = @id
        AND {Item.pickup_date} IS NULL"

        Dim params As New Dictionary(Of String, Object) From {
            {"@pickup_date", claimTime},
            {"@id", _id.Value}
        }

        Try
            Dim affectedRows As Integer = Await ExecuteQueryAsync(sql, params)
            If affectedRows <= 0 Then Return False

            If Not String.IsNullOrWhiteSpace(_courierCmb.SelectedValue) Then

                Dim shippingSql As String =
            $"INSERT INTO {Delivery.table_name}
                ({Delivery.item_id}, {Delivery.courier_id}, {Delivery.shipping_fee})
              VALUES
                (@item_id, @courier_id, @shipping_fee)
              ON DUPLICATE KEY UPDATE
                {Delivery.courier_id}    = VALUES({Delivery.courier_id}),
                {Delivery.shipping_fee}  = VALUES({Delivery.shipping_fee})"

                Dim shippingParams As New Dictionary(Of String, Object) From {
                {"@item_id", _id.Value},
                {"@courier_id", _courierCmb.SelectedValue},
                {"@shipping_fee", _shippingFeePanel.NumericValue}
            }

                Await ExecuteQueryAsync(shippingSql, shippingParams)
            End If

            Return True

        Catch ex As Exception
            MessageBox.Show(ex.Message)
            Return False
        End Try

    End Function

    ' ── Helpers ───────────────────────────────────────────────────────────────

    Private Sub CancelAdd()
        root.rootNav.GoBackPage()
    End Sub

    Private Sub CenterTableFormat(sender As Object, e As EventArgs)
        _tableFormat.Left = (Me.ClientSize.Width - _tableFormat.Width) \ 2
        _tableFormat.Top = (Me.ClientSize.Height - _tableFormat.Height) \ 2
        _tableFormat.Visible = True
    End Sub

End Class