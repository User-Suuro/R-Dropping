Imports System.IO

Public Class Config

    Private Shared ReadOnly FilePath As String =
        Path.Combine(GetSolutionPath(), "config.txt")

    ' LOAD 
    Public Shared Function Load(Of T As New)() As T
        If Not File.Exists(FilePath) Then
            Throw New FileNotFoundException("Config file not found.")
        End If

        Dim instance As New T()
        Dim dict As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

        For Each line In File.ReadAllLines(FilePath)
            If String.IsNullOrWhiteSpace(line) Then Continue For
            If line.Trim().StartsWith("#") Then Continue For
            If Not line.Contains("=") Then Continue For

            Dim parts = line.Split({"="c}, 2)
            dict(parts(0).Trim()) = parts(1).Trim()
        Next

        For Each prop In GetType(T).GetProperties()
            If Not prop.CanWrite Then Continue For

            If dict.ContainsKey(prop.Name) Then
                Dim rawValue = dict(prop.Name)
                Dim convertedValue = ConvertToType(rawValue, prop.PropertyType)
                prop.SetValue(instance, convertedValue)
            End If
        Next

        Return instance
    End Function

    ' SAVE 
    Public Shared Sub Save(Of T)(config As T)

        Dim lines As New List(Of String)

        If File.Exists(FilePath) Then
            lines = File.ReadAllLines(FilePath).ToList()
        End If

        Dim updatedKeys As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim props = GetType(T).GetProperties()

        ' Update existing keys
        For i As Integer = 0 To lines.Count - 1
            Dim line = lines(i)

            If String.IsNullOrWhiteSpace(line) OrElse line.Trim().StartsWith("#") Then Continue For
            If Not line.Contains("=") Then Continue For

            Dim parts = line.Split({"="c}, 2)
            Dim key = parts(0).Trim()

            Dim prop = props.FirstOrDefault(Function(p) String.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
            If prop IsNot Nothing AndAlso prop.CanRead Then
                Dim value = prop.GetValue(config)
                lines(i) = $"{prop.Name}={value}"
                updatedKeys.Add(prop.Name)
            End If
        Next

        ' Append missing keys (no duplicates)
        For Each prop In props
            If Not prop.CanRead Then Continue For
            If updatedKeys.Contains(prop.Name) Then Continue For

            Dim value = prop.GetValue(config)
            lines.Add($"{prop.Name}={value}")
        Next

        File.WriteAllLines(FilePath, lines)

    End Sub

    ' TYPE CONVERSION
    Private Shared Function ConvertToType(value As String, targetType As Type) As Object
        If targetType Is GetType(Integer) Then
            Dim i As Integer
            Integer.TryParse(value, i)
            Return i

        ElseIf targetType Is GetType(Boolean) Then
            Dim b As Boolean
            Boolean.TryParse(value, b)
            Return b

        ElseIf targetType Is GetType(Double) Then
            Dim d As Double
            Double.TryParse(value, d)
            Return d

        Else
            Return value
        End If
    End Function

    Public Shared Function GetSolutionPath() As String
        Dim dir As String = Application.StartupPath
        dir = Directory.GetParent(dir).Parent.Parent.FullName
        Return dir
    End Function


    Public Shared Sub EnsureConfigExists(Of T As New)()

        If File.Exists(FilePath) Then Exit Sub

        Dim instance As New T()
        Dim lines As New List(Of String)

        lines.Add("# Auto-generated configuration file")
        lines.Add("# Modify values as needed")
        lines.Add("")

        For Each prop In GetType(T).GetProperties()
            If Not prop.CanRead Then Continue For

            Dim value = prop.GetValue(instance)

            ' Optional: provide better defaults for Nothing
            If value Is Nothing Then
                If prop.PropertyType Is GetType(String) Then
                    value = ""
                ElseIf prop.PropertyType Is GetType(Integer) OrElse
                       prop.PropertyType Is GetType(Double) Then
                    value = 0
                ElseIf prop.PropertyType Is GetType(Boolean) Then
                    value = False
                End If
            End If

            lines.Add($"{prop.Name}={value}")
        Next

        File.WriteAllLines(FilePath, lines)

    End Sub


    Public Shared Function FindSolutionRoot(
    Optional folder As String = Nothing,
    Optional subFolder As String = Nothing
) As String

        Dim current As New DirectoryInfo(Application.StartupPath)

        Do While current IsNot Nothing

            If current.GetFiles("*.sln").Length > 0 Then

                Dim rootPath As String = current.FullName

                If Not String.IsNullOrWhiteSpace(folder) Then
                    rootPath = Path.Combine(rootPath, folder)
                End If

                If Not String.IsNullOrWhiteSpace(subFolder) Then
                    rootPath = Path.Combine(rootPath, subFolder)
                End If

                Directory.CreateDirectory(rootPath)

                Return rootPath

            End If

            current = current.Parent

        Loop

        Return Nothing

    End Function

    Public Shared Function BuildRelativePath(
    folder As String,
    subFolder As String,
    prefix As String,
    extension As String
) As String

        Dim timestamp As String =
        DateTime.Now.ToString("yyyyMMdd_HHmmss_ffff")

        If Not extension.StartsWith(".") Then
            extension = "." & extension
        End If

        Dim fileName As String =
        $"{prefix}_{timestamp}{extension}"

        Return Path.Combine(
        folder,
        subFolder,
        fileName
    )

    End Function
End Class