Imports MySql.Data.MySqlClient

Public Class DropOffForm
    Inherits BasePanel

    Private _subContainerLeft As PrimaryFlowLayoutPanel
    Private _subContainerRight As PrimaryFlowLayoutPanel
    Private _tableFormat As TableLayoutPanel

    Private _managedCmb As BaseComboBox
    Private _managedField As ValidationPanel

    Private _sellerCmb As BaseComboBox
    Private _sellerField As ValidationPanel

    Private _buyerCmb As BaseComboBox
    Private _buyerField As ValidationPanel

    Private _pricingCmb As BaseComboBox
    Private _pricingField As ValidationPanel
    Private _dailyPricingPlaholder As BaseInputPanel
    Private _basePricingPlacholder As BaseInputPanel

    Private _itemNameInp As BaseInputPanel
    Private _itemNameField As ValidationPanel

    Private _itemDescInp As BaseInputPanel
    Private _itemDescField As ValidationPanel

    Private _imagePanel As BaseImagePanel
    Private _imageField As ValidationPanel

    Private _storedCmb As BaseComboBox
    Private _storedField As ValidationPanel
    Private _storedRemainingPlaceholder As BaseInputPanel

    Private _addbutton As BaseButton
    Private _cancelbutton As BaseButton

    Private _buttontable As TableLayoutPanel
    Private _id As Integer?

    Public Sub New(Optional id As Integer? = Nothing)
        Me.Dock = DockStyle.Fill
        _id = id
        InitializeComponent()
        _tableFormat.Visible = False
        AddHandler Me.Resize, AddressOf centerTableFormat
        AddHandler _tableFormat.SizeChanged, AddressOf centerTableFormat
        centerTableFormat(Nothing, EventArgs.Empty)
        loadasync()
    End Sub

    Public Sub InitializeComponent()

        ' Containers

        _tableFormat = New TableLayoutPanel() With {
            .ColumnCount = 2,
            .RowCount = 2,
            .Padding = New Padding(16),
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Height = 516,
            .Width = 516,
            .BorderStyle = BorderStyle.FixedSingle
        }

        With _tableFormat.ColumnStyles
            .Add(New ColumnStyle(SizeType.Percent, 50))
            .Add(New ColumnStyle(SizeType.Percent, 50))
        End With

        With _tableFormat.RowStyles
            .Add(New RowStyle(SizeType.Percent, 90))
            .Add(New RowStyle(SizeType.Percent, 10))
        End With

        _subContainerLeft = New PrimaryFlowLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .FlowDirection = FlowDirection.TopDown,
            .AutoSize = True,
            .WrapContents = False,
            .AutoScroll = True
        }

        _subContainerRight = New PrimaryFlowLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .FlowDirection = FlowDirection.TopDown,
            .AutoSize = True,
            .WrapContents = False,
            .AutoScroll = True
        }


        ' managed by
        _managedCmb = New BaseComboBox("Managed by") With {
            .Placeholder = "Select Manager"
        }

        _managedField = New ValidationPanel(_managedCmb)
        _managedField.SetValidator(New InputValidator().Required())


        ' seller 

        _sellerCmb = New BaseComboBox("Seller") With {
            .Placeholder = "Select Seller",
            .Width = _managedCmb.Width
        }

        _sellerField = New ValidationPanel(_sellerCmb)
        _sellerField.SetValidator(New InputValidator().Required())


        ' buyer

        _buyerCmb = New BaseComboBox("Buyer") With {
            .Placeholder = "Select Buyer",
            .Width = _managedCmb.Width
        }

        _buyerField = New ValidationPanel(_buyerCmb)
        _buyerField.SetValidator(New InputValidator().Required())


        ' pricing

        Dim _pricingTable = New TableLayoutPanel With {
            .ColumnCount = 2,
            .RowCount = 2,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Width = _subContainerLeft.Width + 8,
            .Height = _buyerCmb.Height * 2 + 10
        }


        With _pricingTable.ColumnStyles
            .Add(New ColumnStyle(SizeType.Percent, 50))
            .Add(New ColumnStyle(SizeType.Percent, 50))
        End With

        With _pricingTable.RowStyles
            .Add(New RowStyle(SizeType.AutoSize))
            .Add(New RowStyle(SizeType.AutoSize))
        End With


        _pricingCmb = New BaseComboBox("Set Pricing") With {
            .Placeholder = "Select Pricing Plan",
            .Dock = DockStyle.Top
        }

        _pricingField = New ValidationPanel(_pricingCmb)
        _pricingField.SetValidator(New InputValidator().Required())

        _basePricingPlacholder = New BaseInputPanel() With {
            .LabelText = "Initial Fee",
            .Enabled = False,
            .Dock = DockStyle.Top
        }

        _dailyPricingPlaholder = New BaseInputPanel() With {
            .LabelText = "Daily Fee",
            .Enabled = False,
            .Dock = DockStyle.Top
        }

        With _pricingTable
            'Row 1
            .Controls.Add(_pricingField, 0, 0)
            .SetColumnSpan(_pricingField, 2)

            'Row 2
            .Controls.Add(_basePricingPlacholder, 0, 1)
            .Controls.Add(_dailyPricingPlaholder, 1, 1)
        End With

        ' item name
        _itemNameInp = New BaseInputPanel() With {
           .LabelText = "Item Name"
        }

        _itemNameField = New ValidationPanel(_itemNameInp)
        _itemNameField.SetValidator(New InputValidator().NoSpecialChar().Required())

        ' item short desc
        _itemDescInp = New BaseInputPanel() With {
            .LabelText = "Short Description (Optional)"
        }

        _itemDescField = New ValidationPanel(_itemDescInp)
        _itemDescField.SetValidator(New InputValidator().NoSpecialChar())


        ' item image path
        _imagePanel = New BaseImagePanel() With {
            .PlaceholderText = "Click to upload...",
            .LabelText = "Item Image",
            .Dock = DockStyle.Top
        }

        _imageField = New ValidationPanel(_imagePanel)
        _imageField.SetValidator(New InputValidator().Required())

        ' item stored on
        Dim _storageTable = New TableLayoutPanel With {
            .ColumnCount = 2,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Width = _subContainerLeft.Width + 8,
            .Height = _buyerCmb.Height * 2 + 8
        }

        With _storageTable.ColumnStyles
            .Add(New ColumnStyle(SizeType.Percent, 60))
            .Add(New ColumnStyle(SizeType.Percent, 40))
        End With

        _storedCmb = New BaseComboBox("Stored at") With {
            .Placeholder = "Select Storage Location",
            .DropdownWidth = _storageTable.Width
        }

        _storedField = New ValidationPanel(_storedCmb)
        _storedField.SetValidator(New InputValidator().Required())

        _storedRemainingPlaceholder = New BaseInputPanel() With {
            .LabelText = "Remaining",
            .Enabled = False
        }

        With _storageTable.Controls
            .Add(_storedField, 0, 0)
            .Add(_storedRemainingPlaceholder, 1, 0)
        End With

        ' button table
        _buttontable = New TableLayoutPanel With {
            .ColumnCount = 2,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Width = _subContainerLeft.Width,
            .Dock = DockStyle.Bottom,
            .Height = 40
        }

        _buttontable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        _buttontable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

        _addbutton = New BaseButton() With {
            .Text = "Add",
            .Dock = DockStyle.Top
        }

        _addbutton.SetPrimary()

        _cancelbutton = New BaseButton() With {
            .Text = "Cancel",
            .Dock = DockStyle.Top
        }

        _cancelbutton.SetDanger()

        ' controls

        With _tableFormat.Controls
            .Add(_subContainerLeft, 0, 0)
            .Add(_subContainerRight, 1, 0)
            .Add(_buttontable, 0, 1)
        End With

        _tableFormat.SetColumnSpan(_buttontable, 2)


        _buttontable.Controls.Add(_cancelbutton, 0, 0)
        _buttontable.Controls.Add(_addbutton, 1, 0)

        With _subContainerLeft.Controls
            .Add(_managedField)
            .Add(_sellerField)
            .Add(_buyerField)
            .Add(_pricingTable)
            .Add(_itemNameField)
            .Add(_itemDescField)
        End With

        With _subContainerRight.Controls
            .Add(_imageField)
            .Add(_storageTable)
        End With


        Me.Controls.Add(_tableFormat)

        ' bind event

        AddHandler _addbutton.Click, AddressOf QueryBuyer
        AddHandler _cancelbutton.Click, AddressOf canceladd

        AddHandler _pricingCmb.SelectedValueChanged, AddressOf PricingChanged
        AddHandler _storedCmb.SelectedValueChanged, AddressOf StorageChanged
    End Sub

    Private Sub PricingChanged(sender As Object, e As EventArgs)
        Dim cmb = DirectCast(sender, BaseComboBox)

        If String.IsNullOrWhiteSpace(cmb.SelectedValue) Then Exit Sub
        Dim selectedVal = (cmb.SelectedValue)

        LoadPricingDetails(selectedVal)
    End Sub

    Private Async Sub StorageChanged(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(_storedCmb.SelectedValue) Then Exit Sub

        Dim remaining = Await GetStorageRemaining(_storedCmb.SelectedValue)

        _storedRemainingPlaceholder.SetValue(remaining.ToString())


        Dim validator = New InputValidator().
        Required().
        MaxValue(remaining, $"Exceeds available storage ({remaining})")

        _storedField.SetValidator(validator)
    End Sub

    Private Async Function GetStorageRemaining(storageId As String) As Task(Of Integer)

        Dim sql As String =
        $"SELECT 
          s.{Storage.capacity_limit} - COUNT(st.{Stored.item_id}) AS remaining
          FROM {Storage.table_name} s
          LEFT JOIN {Stored.table_name} st
          ON st.{Stored.storage_id} = s.{Storage.id}
          WHERE s.{Storage.id} = @id
          GROUP BY s.{Storage.id}, s.{Storage.capacity_limit}"

        Dim params As New Dictionary(Of String, Object) From {
            {"@id", storageId}
        }

        Try
            Using reader = Await ReadQueryAsync(sql, params)

                If reader Is Nothing Then Return 0

                If Await reader.ReadAsync() Then
                    Dim value = reader("remaining")

                    If value IsNot DBNull.Value Then
                        Return Math.Max(0, Convert.ToInt32(value))
                    End If
                End If

            End Using

        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try

        Return 0

    End Function

    Private Async Sub LoadPricingDetails(pricingId As String)

        Dim sql As String =
        $"SELECT {Pricing.base_fee}, {Pricing.daily_increment_fee}
        FROM {Pricing.table_name}
        WHERE {Pricing.id} = @id"

        Dim params As New Dictionary(Of String, Object) From {
            {"@id", pricingId}
        }

        Using reader = Await ReadQueryAsync(sql, params)
            If reader IsNot Nothing AndAlso Await reader.ReadAsync() Then
                _basePricingPlacholder.SetValue(reader(Pricing.base_fee).ToString())
                _dailyPricingPlaholder.SetValue(reader(Pricing.daily_increment_fee).ToString())
            End If
        End Using

    End Sub

    Private Function validateallinputs() As Boolean
        Return {_managedField, _sellerField, _buyerField, _pricingField, _itemNameField, _itemDescField, _imageField, _storedField}.All(Function(f) f.ValidateInput())
    End Function

    Private Async Sub loadasync()
        Await loadDataForInput()
        If _id.HasValue() Then
            ' Await fetchdataforeditmode(_id.Value)
        End If
    End Sub


    Private Async Function loadDataForInput() As Task
        Try
            Dim loadingDlg As New BaseDialog()

            Await DialogTypes.ShowLoadingUntilAsync(
                loadingDlg,
                Form1.Instance,
                Async Function()
                    Await LoadManagers()
                    Await LoadSellers()
                    Await LoadBuyers()
                    Await LoadPricing()
                    Await LoadStorage()
                End Function
            )
        Catch ex As Exception
            MessageBox.Show(ex.Message)
        End Try

    End Function


    Private Async Function LoadManagers() As Task
        Dim list As New List(Of ComboItem)

        Dim sql As String =
        $"SELECT {Employee.id}, CONCAT({Employee.first_name}, ' ', {Employee.last_name}) AS name
        FROM {Employee.table_name}"

        Using reader = Await ReadQueryAsync(sql)
            If reader IsNot Nothing Then
                While Await reader.ReadAsync()
                    list.Add(New ComboItem(
                    reader(Employee.id).ToString(),
                    reader("name").ToString()
                ))
                End While
            End If
        End Using

        _managedCmb.ComboItems = list
    End Function

    Private Async Function LoadSellers() As Task
        Dim list As New List(Of ComboItem)

        Dim sql As String =
        $"SELECT {Seller.id}, {Seller.seller_name}
         FROM {Seller.table_name}"

        Using reader = Await ReadQueryAsync(sql)
            If reader IsNot Nothing Then
                While Await reader.ReadAsync()
                    list.Add(New ComboItem(
                        reader(Seller.id).ToString(),
                        reader(Seller.seller_name).ToString()
                    ))
                End While
            End If
        End Using

        _sellerCmb.ComboItems = list
    End Function

    Private Async Function LoadPricing() As Task
        Dim list As New List(Of ComboItem)

        Dim sql As String =
      $"SELECT {Pricing.id}, {Pricing.rate_label}
      FROM {Pricing.table_name}"

        Using reader = Await ReadQueryAsync(sql)
            If reader IsNot Nothing Then
                While Await reader.ReadAsync()
                    list.Add(New ComboItem(
                    reader(Pricing.id).ToString(),
                    reader(Pricing.rate_label).ToString()
                ))
                End While
            End If
        End Using

        _pricingCmb.ComboItems = list
    End Function


    Private Async Function LoadBuyers() As Task
        Dim list As New List(Of ComboItem)

        Dim sql As String =
        $"SELECT {Buyer.id}, CONCAT({Buyer.first_name}, ' ', {Buyer.last_name}) AS name
        FROM {Buyer.table_name}"

        Using reader = Await ReadQueryAsync(sql)
            If reader IsNot Nothing Then
                While Await reader.ReadAsync()
                    list.Add(New ComboItem(
                    reader(Buyer.id).ToString(),
                    reader("name").ToString()
                ))
                End While
            End If
        End Using

        _buyerCmb.ComboItems = list
    End Function


    Private Async Function LoadStorage() As Task
        Dim list As New List(Of ComboItem)

        Dim sql As String =
        $"SELECT {Storage.id}, {Storage.storage_name}
          FROM {Storage.table_name}"

        Using reader = Await ReadQueryAsync(sql)
            If reader IsNot Nothing Then
                While Await reader.ReadAsync()
                    list.Add(New ComboItem(
                        reader(Storage.id).ToString(),
                        reader(Storage.storage_name).ToString()
                    ))
                End While
            End If
        End Using

        _storedCmb.ComboItems = list
    End Function


    Private Async Sub QueryBuyer()

        If Not validateallinputs() Then
            Exit Sub
        End If

        Dim confirm_dlg = New BaseDialog()

        Dim msg As String = "Are you sure you want to add this Item?"

        If _id.HasValue() Then
            msg = "are you sure you want to save changes to this Item?"
        End If

        DialogTypes.Apply(confirm_dlg,
                 DialogType.Confirmation,
                 "confirmation",
                 msg)

        If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
            Dim loadingdlg As New BaseDialog()

            Dim completed As Boolean = Await DialogTypes.ShowLoadingUntilAsync(
                loadingdlg,
                Form1.Instance,
                Async Function()

                    Dim queryresult As Boolean

                    If _id.HasValue() Then
                        ' queryresult = await editquery()
                    Else
                        queryresult = Await addQuery()
                    End If

                    If queryresult Then
                        Dim info_dlg = New BaseDialog()

                        DialogTypes.Apply(info_dlg,
                          DialogType.Info,
                          "success",
                          "changes was saved successfully")

                        Dim result_info_dlg = Await info_dlg.ShowBaseDialogAsync(Form1.Instance)

                        If result_info_dlg = DialogResultType.Confirm Then
                            root.rootNav.GoBackPage()
                        End If
                    Else
                        Dim error_dlg = New BaseDialog()

                        DialogTypes.Apply(error_dlg,
                          DialogType.Error,
                          "error",
                          "something went wrong")

                        error_dlg.ShowDialog()
                    End If

                End Function
            )
        Else
            confirm_dlg.Hide()
        End If

    End Sub

    Private Async Function addQuery() As Task(Of Boolean)

        Dim itemSql As String =
        $"INSERT INTO {Item.table_name} " &
        $"({Item.managed_by}, {Item.pricing_id}, {Item.buyer_id}, {Item.seller_id}, {Item.name}, {Item.desc}, {Item.img_path}) " &
        $"VALUES (@managed_by, @pricing_id, @buyer_id, @seller_id, @item_name, @description, @img_path)"

        Dim itemParams As New Dictionary(Of String, Object) From {
            {"@managed_by", _managedCmb.SelectedValue},
            {"@pricing_id", _pricingCmb.SelectedValue},
            {"@seller_id", _sellerCmb.SelectedValue},
            {"@buyer_id", _buyerCmb.SelectedValue},
            {"@item_name", _itemNameInp.Value},
            {"@description", ToDbNull(_itemDescInp.Value)},
            {"@img_path", _imagePanel.SaveImage()}
        }

        Try
            Dim affectedRows As Integer = Await ExecuteQueryAsync(itemSql, itemParams)

            If affectedRows <= 0 Then Return False

            Dim newItemId As Integer = 0

            Using reader = Await ReadQueryAsync("SELECT LAST_INSERT_ID()")
                If reader IsNot Nothing AndAlso Await reader.ReadAsync() Then
                    newItemId = Convert.ToInt32(reader(0))
                End If
            End Using

            If newItemId = 0 Then Return False

            Dim storedSql As String =
            $"INSERT INTO {Stored.table_name} " &
            $"({Stored.item_id}, {Stored.storage_id}) " &
            $"VALUES (@item_id, @storage_id)"

            Dim storedParams As New Dictionary(Of String, Object) From {
                {"@item_id", newItemId},
                {"@storage_id", _storedCmb.SelectedValue}
            }

            Dim storedRows As Integer = Await ExecuteQueryAsync(storedSql, storedParams)
            Return storedRows > 0

        Catch ex As Exception
            MessageBox.Show(ex.Message)
            Return False
        End Try

    End Function




    'private async function fetchdataforeditmode(id as integer) as task
    '    _addbutton.text = "save"

    '    dim sql as string =
    '   $"select {courier.first_name}, {courier.last_name}, {courier.vehicle_type}, {courier.vehicle_brand}, {courier.plate_no} " &
    '   $"from {courier.table_name} " &
    '   $"where {courier.id} = @{courier.id}"

    '    dim params as new dictionary(of string, object) from {
    '    {$"@{courier.id}", id}
    '}

    '    dim reader as mysqldatareader = await readqueryasync(sql, params)

    '    if reader isnot nothing then
    '        while await reader.readasync()
    '            dim firstname as string = reader(courier.first_name).tostring()
    '            dim lastname as string = reader(courier.last_name).tostring()
    '            dim vehicle_type as string = reader(courier.vehicle_type).tostring()
    '            dim vehicle_brand as string = reader(courier.vehicle_brand).tostring()
    '            dim plate_no as string = reader(courier.plate_no).tostring()

    '            _firstnameinput.setvalue(firstname)
    '            _lastnameinput.setvalue(lastname)
    '            _vehicletypeinput.setvalue(vehicle_type)
    '            _vehiclebrandinput.setvalue(vehicle_brand)
    '            _platenoinput.setvalue(plate_no)
    '        end while

    '        reader.close()
    '    end if
    'end function

    'private async function editquery() as task(of boolean)
    '    dim sql as string =
    '   $"update {courier.table_name} set " &
    '   $"{courier.first_name} = @{courier.first_name}, " &
    '   $"{courier.last_name} = @{courier.last_name}, " &
    '   $"{courier.vehicle_type} = @{courier.vehicle_type}, " &
    '   $"{courier.vehicle_brand} = @{courier.vehicle_brand}, " &
    '   $"{courier.plate_no} = @{courier.plate_no} " &
    '   $"where {courier.id} = @{courier.id}"


    '    dim params as new dictionary(of string, object) from {
    '    {$"@{courier.first_name}", _firstnameinput.value},
    '    {$"@{courier.last_name}", _lastnameinput.value},
    '    {$"@{courier.vehicle_type}", _vehicletypeinput.selectedvalue},
    '    {$"@{courier.vehicle_brand}", _vehiclebrandinput.getvalue()},
    '    {$"@{courier.plate_no}", _platenoinput.value},
    '    {$"@{courier.id}", _id}
    '    }

    '    dim affectedrows as integer = await executequeryasync(sql, params)
    '    if affectedrows > 0 then
    '        return true
    '    end if
    '    return false
    'end function


    Private Sub canceladd()
        root.rootNav.GoBackPage()
    End Sub

    Private Sub centerTableFormat(sender As Object, e As EventArgs)
        _tableFormat.Left = (Me.ClientSize.Width - _tableFormat.Width) \ 2
        _tableFormat.Top = (Me.ClientSize.Height - _tableFormat.Height) \ 2
        _tableFormat.Visible = True
    End Sub


End Class
