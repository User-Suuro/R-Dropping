Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

Module session
    Public SessionUserID As Integer = 0
    Public SessionEmail As String = String.Empty
    Public SessionPassword As String = String.Empty
    Public SessionPosition As String = String.Empty
    Public IsLoggedIn As Boolean = False

    Public Function Login(ByVal id As Integer,
                          ByVal email As String,
                          ByVal password As String,
                          ByVal position As String) As Boolean
        Try
            SessionUserID = id
            SessionEmail = email
            SessionPassword = Encrypt(password)   ' store encrypted
            SessionPosition = position
            IsLoggedIn = True
            Return True
        Catch ex As Exception
            MsgBox("Login Error: " & ex.Message, MsgBoxStyle.Critical, "Login Failed")
            Return False
        End Try
    End Function


    Public Sub Logout()
        SessionUserID = 0
        SessionEmail = String.Empty
        SessionPassword = String.Empty
        SessionPosition = String.Empty
        IsLoggedIn = False
    End Sub

    Public Function GetPassword() As String
        If String.IsNullOrEmpty(SessionPassword) Then Return String.Empty
        Return Decrypt(SessionPassword)
    End Function

    Public Function Encrypt(ByVal clearText As String) As String
        Dim EncryptionKey As String = "MAKV2SPBNI99212"
        Dim clearBytes As Byte() = Encoding.Unicode.GetBytes(clearText)
        Using encryptor As Aes = Aes.Create()
            Dim pdb As New Rfc2898DeriveBytes(EncryptionKey, New Byte() {&H49, &H76, &H61, &H6E, &H20, &H4D,
             &H65, &H64, &H76, &H65, &H64, &H65,
             &H76})
            encryptor.Key = pdb.GetBytes(32)
            encryptor.IV = pdb.GetBytes(16)
            Using ms As New MemoryStream()
                Using cs As New CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write)
                    cs.Write(clearBytes, 0, clearBytes.Length)
                    cs.Close()
                End Using
                clearText = Convert.ToBase64String(ms.ToArray())
            End Using
        End Using
        Return clearText
    End Function


    Public Function Decrypt(ByVal cipherText As String) As String
        Dim EncryptionKey As String = "MAKV2SPBNI99212"
        Dim cipherBytes As Byte() = Convert.FromBase64String(cipherText)
        Using encryptor As Aes = Aes.Create()
            Dim pdb As New Rfc2898DeriveBytes(EncryptionKey, New Byte() {&H49, &H76, &H61, &H6E, &H20, &H4D,
             &H65, &H64, &H76, &H65, &H64, &H65,
             &H76})
            encryptor.Key = pdb.GetBytes(32)
            encryptor.IV = pdb.GetBytes(16)
            Using ms As New MemoryStream()
                Using cs As New CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write)
                    cs.Write(cipherBytes, 0, cipherBytes.Length)
                    cs.Close()
                End Using
                cipherText = Encoding.Unicode.GetString(ms.ToArray())
            End Using
        End Using
        Return cipherText
    End Function

End Module