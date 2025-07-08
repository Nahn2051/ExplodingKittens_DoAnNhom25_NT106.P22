using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Firebase.Database;

public class FirebaseLoginManager : MonoBehaviour
{
    // Đăng kí tài khoản
    [Header(header: "Register Account")]
    public InputField idRegisterEmail;
    public InputField idRegisterPassword;

    public Button buttonRegister;

    //Đăng nhập tài khoản
    [Header(header: "Login Account")]
    public InputField idLoginEmail;
    public InputField idLoginPassword;

    public Button buttonLogin;

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    // Chuyển đổi qua lại giữa đăng nhập và đăng ký
    [Header(header: "Switch")]
    public Button buttonMovetoRegister;
    public Button buttonMovetoLogin;
    public Button buttonForgotPassword;

    public GameObject loginForm;
    public GameObject registerForm;
    public GameObject ForgotPasswordForm;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        buttonRegister.onClick.AddListener(RegisterAccount);
        buttonLogin.onClick.AddListener(LoginAccount);
        buttonMovetoRegister.onClick.AddListener(MoveToRegister);
        buttonMovetoLogin.onClick.AddListener(MoveToLogin);
        buttonForgotPassword.onClick.AddListener(MoveToForgotPassword);
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // Set default UI panel
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        forgotPasswordPanel.SetActive(false);
        // Bind button listeners
        registerButton.onClick.AddListener(RegisterAccount);
        loginButton.onClick.AddListener(LoginAccount);
        sendResetPasswordButton.onClick.AddListener(SendPasswordResetEmail);
    }

    public void ShowPopup(string message)
    {
        popupText.text = message;
        popupPanel.SetActive(true);
    }

    public void HidePopup()
    {
        popupPanel.SetActive(false);
    }

    private bool IsValidEmail(string email)
    {
        return email.Contains("@") && email.Contains(".");
    }

    public void RegisterAccount()
    {
        string email = idRegisterEmail.text;
        string password = idRegisterPassword.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.Log("Email or Password is empty");
            return;
        }
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.Log("Create user failed.");
                return;
            }
            Debug.Log(message: "Firebase user created successfully");
        });
    }

    public void LoginAccount()
    {
        string email = idLoginEmail.text;
        string password = idLoginPassword.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.Log("Email or Password is empty");
            return;
        }
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Sign in was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log(message: "Sign in encountered an error");
                return;
            }
            Debug.Log(message: "Firebase user signed in successfully");
            FirebaseUser newUser = task.Result.User;

            // Chuyển cảnh vào game
            SceneManager.LoadScene(sceneName: "Main Menu");

            // 1. Tạo một đường dẫn trong database để lưu trạng thái online của user
            DatabaseReference sessionRef = dbReference.Child("sessions").Child(user.UserId).Child("isOnline");

            // 2. Kiểm tra xem user này đã online ở nơi khác chưa
            sessionRef.GetValueAsync().ContinueWithOnMainThread(checkTask =>
            {
                if (checkTask.IsFaulted || checkTask.IsCanceled)
                {
                    ShowPopup("Failed to check session status.");
                    auth.SignOut();
                    return;
                }

                DataSnapshot snapshot = checkTask.Result;
                if (snapshot.Exists && (bool)snapshot.Value == true)
                {
                    // Nếu đã online -> hiển thị lỗi và đăng xuất
                    ShowPopup("Account is already logged in elsewhere.");
                    auth.SignOut();
                }
                else
                {
                    // Nếu chưa online -> cho phép đăng nhập và cập nhật trạng thái
                    // Dùng OnDisconnect để tự động set false khi người dùng mất kết nối
                    sessionRef.OnDisconnect().SetValue(false);
                    // Đặt trạng thái là true
                    sessionRef.SetValueAsync(true);

                    // Chuyển đến màn hình Main Menu
                    SceneManager.LoadScene("Main Menu");
                }
            });
        });
    }
    public void MoveToRegister()
    {
        loginForm.SetActive(false);
        registerForm.SetActive(true);
    }
    public void MoveToLogin()
    {
        loginForm.SetActive(true);
        registerForm.SetActive(false);
    }
    public void MoveToForgotPassword()  
    {
        loginForm.SetActive(false);
        registerForm.SetActive(false);
        ForgotPasswordForm.SetActive(true);
    }
}
