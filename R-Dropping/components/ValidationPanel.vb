Imports System.Text.RegularExpressions

Public Interface IValueProvider
    ReadOnly Property Value As String
    Event ValueChanged As EventHandler
End Interface

Public Interface IValidationStyleable
    Sub OnValidationError()
    Sub OnValidationClear()
End Interface

Public Interface IValidatableInput
    Sub SetValidator(validator As InputValidator)
    Function ValidateInput() As Boolean
    ReadOnly Property IsValid As Boolean
End Interface

Public Class ValidationPanel
    Inherits Panel
    Implements IValidatableInput

    Private _host As ValidationHost
    Private _lblError As BaseLabel
    Private _content As Control
    Private _isValid As Boolean = True
    Private _hasInteracted As Boolean = False


    Private Const ErrorHeight As Integer = 14
    Private Const ErrorTopGap As Integer = 0

    Public ReadOnly Property IsValid As Boolean Implements IValidatableInput.IsValid
        Get
            Return _isValid
        End Get
    End Property


    Public Sub New(content As Control)
        _content = content

        Me.BackColor = Color.Transparent
        Me.AutoSize = False

        ' Error label 
        _lblError = New BaseLabel() With {
            .AutoSize = False,
            .Height = ErrorHeight,
            .ForeColor = Color.Red,
            .TextAlign = ContentAlignment.TopRight,
            .Visible = False
        }

        _lblError.SetXS()

        Controls.Add(content)
        Controls.Add(_lblError)

        Me.Height = content.Height

        '  Wire host
        Dim getValue As Func(Of String)

        If TypeOf content Is IValueProvider Then
            Dim provider = DirectCast(content, IValueProvider)
            getValue = Function() provider.Value
            AddHandler provider.ValueChanged,
            Sub(s As Object, e As EventArgs)
                _hasInteracted = True
                If _host IsNot Nothing Then _host.ValidateInput()
            End Sub
        Else
            getValue = Function() String.Empty
        End If
        _host = New ValidationHost(getValue, AddressOf ShowError, AddressOf ClearError)

        ' Layout events 
        AddHandler Me.Resize, AddressOf LayoutChildren
        AddHandler content.Resize, AddressOf LayoutChildren
        LayoutChildren(Nothing, EventArgs.Empty)
    End Sub

    '  Layout 

    Private Sub LayoutChildren(sender As Object, e As EventArgs)
        _content.Location = New Point(0, 0)
        _content.Width = Me.Width

        _lblError.Location = New Point(0, _content.Height + ErrorTopGap)
        _lblError.Width = Me.Width
    End Sub

    '  Error display 

    Private Sub ShowError(message As String)
        _lblError.Text = message
        _lblError.Visible = True
        Me.Height = _content.Height + ErrorTopGap + ErrorHeight

        If TypeOf _content Is IValidationStyleable Then
            DirectCast(_content, IValidationStyleable).OnValidationError()
        End If

        _isValid = False
    End Sub

    Private Sub ClearError()
        _lblError.Visible = False
        Me.Height = _content.Height

        If TypeOf _content Is IValidationStyleable Then
            DirectCast(_content, IValidationStyleable).OnValidationClear()
        End If

        _isValid = True
    End Sub

    '  IValidatableInput 

    Public Sub SetValidator(validator As InputValidator) Implements IValidatableInput.SetValidator
        _host.SetValidator(validator)
    End Sub

    Public Function ValidateInput() As Boolean Implements IValidatableInput.ValidateInput
        Return _host.ValidateInput()
    End Function




End Class


Public Class ValidationHost

    Private _validator As InputValidator
    Private ReadOnly _getValue As Func(Of String)
    Private ReadOnly _onError As Action(Of String)
    Private ReadOnly _onClear As Action

    Public Sub New(getValue As Func(Of String),
                   onError As Action(Of String),
                   onClear As Action)
        _getValue = getValue
        _onError = onError
        _onClear = onClear
    End Sub

    Public Sub SetValidator(validator As InputValidator)
        _validator = validator
    End Sub

    Public Function ValidateInput() As Boolean
        If _validator Is Nothing Then
            _onClear()
            Return True
        End If

        Dim result = _validator.Validate(_getValue())

        If Not result.IsValid Then
            _onError(result.ErrorMessage)
            Return False
        End If

        _onClear()
        Return True
    End Function

End Class


Public Class InputValidator

    Private ReadOnly _rules As New List(Of Func(Of String, ValidationResult))

    Public Function Required() As InputValidator
        _rules.Add(Function(v)
                       Return If(String.IsNullOrWhiteSpace(v),
                                 New ValidationResult(False, "This field is required"),
                                 ValidationResult.Ok)
                   End Function)
        Return Me
    End Function

    Public Function MinLength(length As Integer) As InputValidator
        _rules.Add(Function(v)
                       Return If(v Is Nothing OrElse v.Length < length,
                                 New ValidationResult(False, $"Minimum length is {length}"),
                                 ValidationResult.Ok)
                   End Function)
        Return Me
    End Function

    Public Function MaxLength(length As Integer) As InputValidator
        _rules.Add(Function(v)
                       Return If(v IsNot Nothing AndAlso v.Length > length,
                                 New ValidationResult(False, $"Maximum length is {length}"),
                                 ValidationResult.Ok)
                   End Function)
        Return Me
    End Function

    Public Function IsNumber() As InputValidator
        _rules.Add(Function(v)
                       Dim n As Double
                       Return If(Not Double.TryParse(v, n),
                                 New ValidationResult(False, "Must be a valid number"),
                                 ValidationResult.Ok)
                   End Function)
        Return Me
    End Function

    Public Function MinValue(min As Double) As InputValidator
        _rules.Add(Function(v)
                       Dim n As Double
                       If Not Double.TryParse(v, n) Then Return New ValidationResult(False, "Must be a valid number")
                       Return If(n < min, New ValidationResult(False, $"Minimum value is {min}"), ValidationResult.Ok)
                   End Function)
        Return Me
    End Function

    Public Function MinValue(min As Double, Optional message As String = Nothing) As InputValidator
        _rules.Add(Function(v)
                       Dim n As Double
                       If Not Double.TryParse(v, n) Then Return New ValidationResult(False, "Must be a valid number")
                       Return If(n < min,
                             New ValidationResult(False, If(message, $"Minimum value is {min}")),
                             ValidationResult.Ok)
                   End Function)
        Return Me
    End Function

    Public Function MaxValue(max As Double) As InputValidator
        _rules.Add(Function(v)
                       Dim n As Double
                       If Not Double.TryParse(v, n) Then Return New ValidationResult(False, "Must be a valid number")
                       Return If(n > max, New ValidationResult(False, $"Maximum value is {max}"), ValidationResult.Ok)
                   End Function)
        Return Me
    End Function

    Public Function MaxValue(max As Decimal,
                        Optional message As String = Nothing) As InputValidator

        _rules.Add(Function(v)
                       If String.IsNullOrWhiteSpace(v) Then Return ValidationResult.Ok

                       Dim num As Decimal
                       If Not Decimal.TryParse(v, num) Then
                           Return New ValidationResult(False, "Invalid number")
                       End If

                       If num > max Then
                           Return New ValidationResult(False,
                           If(message, $"Must not exceed {max}"))
                       End If

                       Return ValidationResult.Ok
                   End Function)

        Return Me
    End Function

    Public Function IsPhone() As InputValidator
        _rules.Add(Function(v)
                       If String.IsNullOrWhiteSpace(v) Then Return ValidationResult.Ok
                       Dim cleaned = Regex.Replace(v, "[\s\-\(\)\+\.]", "")
                       Return If(Not Regex.IsMatch(cleaned, "^\d{7,15}$"),
                             New ValidationResult(False, "Must be a valid phone number"),
                             ValidationResult.Ok)
                   End Function)
        Return Me
    End Function

    Public Function IsEmail() As InputValidator
        _rules.Add(Function(v)
                       If String.IsNullOrWhiteSpace(v) Then Return ValidationResult.Ok
                       Return If(Not Regex.IsMatch(v, "^[^@\s]+@[^@\s]+\.[^@\s]+$"),
                             New ValidationResult(False, "Must be a valid email address"),
                             ValidationResult.Ok)
                   End Function)
        Return Me
    End Function

    Public Function NoSpecialChar(Optional message As String = "Contains invalid characters") As InputValidator
        _rules.Add(Function(v)
                       If String.IsNullOrWhiteSpace(v) Then Return ValidationResult.Ok

                       Dim forbiddenPattern As String = "[\'\"";]"

                       Return If(Regex.IsMatch(v, forbiddenPattern),
                             New ValidationResult(False, message),
                             ValidationResult.Ok)
                   End Function)

        Return Me
    End Function


    Public Function Custom(rule As Func(Of String, ValidationResult)) As InputValidator
        _rules.Add(rule)
        Return Me
    End Function



    Public Function Validate(value As String) As ValidationResult
        For Each rule In _rules
            Dim result = rule.Invoke(value)
            If Not result.IsValid Then Return result
        Next
        Return ValidationResult.Ok
    End Function

End Class


Public Class ValidationResult

    Public Property IsValid As Boolean
    Public Property ErrorMessage As String

    Public Sub New(valid As Boolean, message As String)
        IsValid = valid
        ErrorMessage = message
    End Sub


    Public Shared ReadOnly Ok As New ValidationResult(True, String.Empty)

End Class