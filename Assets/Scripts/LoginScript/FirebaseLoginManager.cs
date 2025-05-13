using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirebaseLoginManager : MonoBehaviour
{
    // Đăng kí tài khoản
    [Header(header: "Register Account")]
    public InputField idRegisterEmail;
    public InputField idRegisterPassword;
    public InputField idRegisterConfirmPassword;
    public Button buttonRegister;

    // Đăng nhập tài khoản
    [Header(header: "Login Account")]
    public InputField idLoginEmail;
    public InputField idLoginPassword;
    public Button buttonLogin;

    // Quên mật khẩu
    [Header(header: "Forgot Password")]
    public InputField idForgotPasswordEmail;
    public Button buttonForgotPasswordSend;

    private FirebaseAuth auth;

    // Chuyển đổi qua lại giữa đăng nhập, đăng ký và quên mật khẩu 
    [Header(header: "Switch")]
    public Button buttonMovetoRegister;
    public Button buttonMovetoLogin;
    public Button buttonForgotPassword;

    public GameObject loginForm;
    public GameObject registerForm;
    public GameObject ForgotPasswordForm;

    // Popup UI
    [Header(header: "Popup UI")]
    public GameObject popupPanel;
    public Text popupText;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        buttonRegister.onClick.AddListener(RegisterAccount);
        buttonLogin.onClick.AddListener(LoginAccount);
        buttonMovetoRegister.onClick.AddListener(MoveToRegister);
        buttonMovetoLogin.onClick.AddListener(MoveToLogin);
        buttonForgotPassword.onClick.AddListener(MoveToForgotPassword);
        buttonForgotPasswordSend.onClick.AddListener(SendForgotPasswordEmail);
    }

    public void ShowPopup(string message)
    {
        popupText.text = message;
        popupPanel.SetActive(true);
    }

    public void RegisterAccount()
    {
        string email = idRegisterEmail.text;
        string password = idRegisterPassword.text;
        string confirmPassword = idRegisterConfirmPassword.text;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
{
            ShowPopup("Vui lòng nhập đầy đủ email, mật khẩu và xác nhận mật khẩu.");
            return;
        }
        if (password != idRegisterConfirmPassword.text)
        {
            ShowPopup("Mật khẩu và xác nhận mật khẩu không khớp.");
            return;
        }
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.Log("Create user failed.");
                ShowPopup("Tạo tài khoản thất bại. Vui lòng thử lại.");
                return;
            }

            FirebaseUser newUser = task.Result.User;

            // Gửi email xác nhận
            newUser.SendEmailVerificationAsync().ContinueWithOnMainThread(verifyTask =>
            {
                if (verifyTask.IsCanceled || verifyTask.IsFaulted)
                {
                    Debug.Log("Send verification email failed.");
                    ShowPopup("Gửi email xác minh thất bại. Vui lòng thử lại sau.");
                    return;
                }

                Debug.Log("Verification email sent to " + newUser.Email);
                ShowPopup("Đăng ký thành công!\nVui lòng kiểm tra email để xác minh tài khoản.");
            });
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
                ShowPopup("Đăng nhập thất bại. Vui lòng thử lại.");
                Debug.Log(message: "Sign in encountered an error");
                return;
            }

            Debug.Log(message: "Firebase user signed in successfully");
            FirebaseUser newUser = task.Result.User;

            if (!newUser.IsEmailVerified)
            {
                ShowPopup("Email chưa được xác minh.\nVui lòng kiểm tra hộp thư để xác minh trước khi đăng nhập.");
                auth.SignOut(); // Đăng xuất ngay
                return;
            }

            // Cho phép vào game nếu đã xác minh
            SceneManager.LoadScene("Main Menu");
        });
    }

    public void SendForgotPasswordEmail()
    {
        string email = idForgotPasswordEmail.text;
        if (string.IsNullOrEmpty(email))
        {
            ShowPopup("Vui lòng nhập email để gửi yêu cầu đặt lại mật khẩu.");
            return;
        }
        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.Log("Send password reset email failed.");
                ShowPopup("Gửi yêu cầu đặt lại mật khẩu thất bại. Vui lòng thử lại.");
                return;
            }
            Debug.Log("Password reset email sent to " + email);
            ShowPopup("Yêu cầu đặt lại mật khẩu đã được gửi đến email của bạn.");
            MoveToLogin();
        });
    }

    public void MoveToRegister()
    {
        loginForm.SetActive(false);
        ForgotPasswordForm.SetActive(false);
        registerForm.SetActive(true);
    }

    public void MoveToLogin()
    {
        loginForm.SetActive(true);
        ForgotPasswordForm.SetActive(false);
        registerForm.SetActive(false);
    }

    public void MoveToForgotPassword()
    {
        loginForm.SetActive(false);
        registerForm.SetActive(false);
        ForgotPasswordForm.SetActive(true);
    }
}
