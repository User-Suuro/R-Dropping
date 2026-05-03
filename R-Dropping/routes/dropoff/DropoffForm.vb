Imports MySql.Data.MySqlClient

Public Class DropOffForm
    Inherits BasePanel

    Private _subcontainer As PrimaryFlowLayoutPanel

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
        AddHandler Me.Resize, AddressOf centersubcontainer
        AddHandler _subcontainer.SizeChanged, AddressOf centersubcontainer
        'loadasync()
    End Sub

    Public Sub InitializeComponent()
        _subcontainer = New PrimaryFlowLayoutPanel() With {
            .AutoSize = True,
            .FlowDirection = FlowDirection.TopDown,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .BackColor = Color.White,
            .Padding = New Padding(16),
            .BorderStyle = BorderStyle.FixedSingle
        }

        ' managed by
        _managedCmb = New BaseComboBox("Managed by") With {
            .Placeholder = "Select Manager"
        }

        _managedField = New ValidationPanel(_managedCmb)
        _managedField.SetValidator(New InputValidator().Required())


        ' seller 

        _sellerCmb = New BaseComboBox("Seller") With {
            .Placeholder = "Select Seller"
        }

        _sellerField = New ValidationPanel(_sellerCmb)
        _sellerField.SetValidator(New InputValidator().Required())


        ' buyer

        _buyerCmb = New BaseComboBox("Buyer") With {
            .Placeholder = "Select Buyer"
        }

        _buyerField = New ValidationPanel(_buyerCmb)
        _buyerField.SetValidator(New InputValidator().Required)


        ' pricing

        Dim _pricingTable = New TableLayoutPanel With {
            .ColumnCount = 3,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Width = _subcontainer.Width,
            .Height = _buyerCmb.Height + 8
        }

        With _pricingTable.ColumnStyles
            .Add(New ColumnStyle(SizeType.Percent, 60))
            .Add(New ColumnStyle(SizeType.Percent, 20))
            .Add(New ColumnStyle(SizeType.Percent, 20))
        End With


        _pricingCmb = New BaseComboBox("Set Pricing") With {
            .Placeholder = "Select Pricing Plan",
            .DropdownWidth = 300
        }

        _pricingField = New ValidationPanel(_pricingCmb)
        _pricingField.SetValidator(New InputValidator().Required())

        _basePricingPlacholder = New BaseInputPanel() With {
            .LabelText = "Initial Fee",
            .Enabled = False
        }

        _dailyPricingPlaholder = New BaseInputPanel() With {
            .LabelText = "Daily Increment Fee",
            .Enabled = False
        }

        With _pricingTable.Controls
            .Add(_pricingField, 0, 0)
            .Add(_basePricingPlacholder, 1, 0)
            .Add(_dailyPricingPlaholder, 2, 0)
        End With


        ' item name


        ' item short desc

        ' item image path

        ' item stored on



        ' button table
        _buttontable = New TableLayoutPanel With {
            .ColumnCount = 2,
            .Padding = Padding.Empty,
            .Margin = New Padding(0, 4, 0, 0),
            .CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            .Width = _subcontainer.Width,
            .Height = 40
        }

        _buttontable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        _buttontable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

        _addbutton = New BaseButton() With {
            .Text = "add",
            .Dock = DockStyle.Top
        }

        _addbutton.SetPrimary()

        _cancelbutton = New BaseButton() With {
            .Text = "cancel",
            .Dock = DockStyle.Top
        }

        _cancelbutton.SetDanger()

        ' controls

        _buttontable.Controls.Add(_cancelbutton, 0, 0)
        _buttontable.Controls.Add(_addbutton, 1, 0)

        With _subcontainer.Controls
            .Add(_managedField)
            .Add(_sellerField)
            .Add(_buyerField)
            .Add(_pricingTable)
            .Add(_buttontable)
        End With

        Me.Controls.Add(_subcontainer)

        ' bind event

        ' AddHandler _addbutton.Click, AddressOf querybuyer
        AddHandler _cancelbutton.Click, AddressOf canceladd
            End Sub

    'private async sub loadasync()
    '    await loaddataforcmb()
    '    if _id.hasvalue() then
    '        await fetchdataforeditmode(_id.value)
    '    end if
    'end sub

    Private Sub canceladd()
        root.rootNav.GoBackPage()
    End Sub

    'Private Async Sub querybuyer()

    '    If Not validateallinputs() Then
    '        Exit Sub
    '    End If

    '    Dim confirm_dlg = New BaseDialog()

    '    Dim msg As String = "are you sure you want to add this courier?"

    '    If _id.HasValue() Then
    '        msg = "are you sure you want to save changes to this courier?"
    '    End If

    '    DialogTypes.Apply(confirm_dlg,
    '             DialogType.Confirmation,
    '             "confirmation",
    '             msg)

    '    If Await confirm_dlg.ShowBaseDialogAsync(Form1.Instance) = DialogResultType.Confirm Then
    '        Dim loadingdlg As New BaseDialog()

    '        Dim completed As Boolean = Await DialogTypes.ShowLoadingUntilAsync(
    '            loadingdlg,
    '            Form1.Instance,
    '            Async Function()

    '                Dim queryresult As Boolean

    '                If _id.HasValue() Then
    '                    ' queryresult = await editquery()
    '                Else
    '                    queryresult = Await addquery()
    '                End If

    '                If queryresult Then
    '                    Dim info_dlg = New BaseDialog()

    '                    DialogTypes.Apply(info_dlg,
    '                      DialogType.Info,
    '                      "success",
    '                      "changes was saved successfully")

    '                    Dim result_info_dlg = Await info_dlg.ShowBaseDialogAsync(Form1.Instance)

    '                    If result_info_dlg = DialogResultType.Confirm Then
    '                        root.rootNav.GoBackPage()
    '                    End If
    '                Else
    '                    Dim error_dlg = New BaseDialog()

    '                    DialogTypes.Apply(error_dlg,
    '                      DialogType.Error,
    '                      "error",
    '                      "something went wrong")

    '                    error_dlg.ShowDialog()
    '                End If

    '            End Function
    '        )
    '    Else
    '        confirm_dlg.Hide()
    '    End If

    'End Sub

    'Private Async Function loaddataforinput() As Task
    '    Dim list As New List(Of String)

    '    ' employee

    '    ' buyer

    '    ' seller

    '    ' storage

    '    ' pricing


    '    Dim sql As String =
    '    $"select distinct {Courier.vehicle_brand} " &
    '    $"from {Courier.table_name}"

    '    Using reader As MySqlDataReader = Await ReadQueryAsync(sql)
    '        If reader IsNot Nothing Then
    '            While Await reader.ReadAsync()
    '                list.Add(reader(Courier.vehicle_brand).ToString())
    '            End While
    '        End If
    '    End Using

    '    _vehiclebrandinput.items = list

    'End Function

    'Private Function validateallinputs() As Boolean
    '    Return {}.All(Function(f) f.validateinput())
    'End Function

    'Private Async Function addquery() As Task(Of Boolean)
    '    Dim sql As String =
    '    $"insert into {Courier.table_name} " &
    '    $"({Courier.first_name}, {Courier.last_name}, {Courier.vehicle_type}, {Courier.vehicle_brand}, {Courier.plate_no}) " &
    '    $"values (@{Courier.first_name}, @{Courier.last_name}, @{Courier.vehicle_type}, @{Courier.vehicle_brand}, @{Courier.plate_no})"

    '    Dim params As New Dictionary(Of String, Object) From {
    '    {$"@{Courier.first_name}", _firstnameinput.value},
    '    {$"@{Courier.last_name}", _lastnameinput.value},
    '    {$"@{Courier.vehicle_type}", _vehicletypeinput.selectedvalue},
    '    {$"@{Courier.vehicle_brand}", _vehiclebrandinput.getvalue()},
    '    {$"@{Courier.plate_no}", ToDbNull(_platenoinput.value)}
    '    }

    '    Dim affectedrows As Integer = Await ExecuteQueryAsync(sql, params)

    '    If affectedrows > 0 Then
    '        Return True
    '    End If

    '    Return False
    'End Function


    Private Sub centersubcontainer(sender As Object, e As EventArgs)
        _subcontainer.Left = (Me.ClientSize.Width - _subcontainer.Width) \ 2
        _subcontainer.Top = (Me.ClientSize.Height - _subcontainer.Height) \ 2
    End Sub


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


End Class
