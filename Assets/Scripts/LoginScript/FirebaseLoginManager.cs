using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FirebaseLoginManager : MonoBehaviour
{
    private FirebaseAuth auth;

    [Header("Register Account")]
    public InputField registerEmailInput;
    public InputField registerPasswordInput;
    public InputField registerConfirmPasswordInput;
    public Button registerButton;

    [Header("Login Account")]
    public InputField loginEmailInput;
    public InputField loginPasswordInput;
    public Button loginButton;

    [Header("Forgot Password")]
    public InputField forgotPasswordEmailInput;
    public Button sendResetPasswordButton;

    [Header("Switch Forms")]
    public Button switchToRegisterButton;
    public Button switchToLoginButton;
    public Button switchToForgotPasswordButton;
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject forgotPasswordPanel;

    [Header("Popup UI")]
    public GameObject popupPanel;
    public Text popupText;

    [Header("Password Toggle - Login")]
    public GameObject loginEyeShow;
    public GameObject loginEyeHide;
    private bool isLoginPasswordVisible = false;

    [Header("Password Toggle - Register")]
    public GameObject registerEyeShow;
    public GameObject registerEyeHide;
    private bool isRegisterPasswordVisible = false;

    [Header("Password Toggle - Confirm")]
    public GameObject registerConfirmEyeShow;
    public GameObject registerConfirmEyeHide;
    private bool isRegisterConfirmPasswordVisible = false;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

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
        string email = registerEmailInput.text;
        string password = registerPasswordInput.text;
        string confirmPassword = registerConfirmPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowPopup("Please fill in all fields.");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowPopup("Invalid email format.");
            return;
        }

        if (password != confirmPassword)
        {
            ShowPopup("Password and confirmation do not match.");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                ShowPopup("Failed to create account.");
                return;
            }

            FirebaseUser newUser = task.Result.User;
            newUser.SendEmailVerificationAsync().ContinueWithOnMainThread(verifyTask =>
            {
                if (verifyTask.IsCanceled || verifyTask.IsFaulted)
                {
                    ShowPopup("Failed to send verification email.");
                    return;
                }
                ShowPopup("Registration successful! Please check your email to verify your account.");
            });
        });
    }

    public void LoginAccount()
    {
        string email = loginEmailInput.text;
        string password = loginPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowPopup("Please enter both email and password.");
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                ShowPopup("Login failed. Please check your credentials.");
                return;
            }

            FirebaseUser user = task.Result.User;
            if (!user.IsEmailVerified)
            {
                ShowPopup("Email is not verified.");
                auth.SignOut();
                return;
            }

            SceneManager.LoadScene("Main Menu");
        });
    }

    public void SendPasswordResetEmail()
    {
        string email = forgotPasswordEmailInput.text;

        if (string.IsNullOrEmpty(email))
        {
            ShowPopup("Please enter your email address.");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowPopup("Invalid email format.");
            return;
        }

        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                ShowPopup("Failed to send reset password email.");
                return;
            }

            ShowPopup("Password reset email sent. Please check your inbox.");
        });
    }

    public void ToggleLoginPasswordVisibility()
    {
        isLoginPasswordVisible = !isLoginPasswordVisible;
        loginPasswordInput.contentType = isLoginPasswordVisible ? InputField.ContentType.Standard : InputField.ContentType.Password;
        loginPasswordInput.ForceLabelUpdate();

        loginEyeShow.SetActive(isLoginPasswordVisible);
        loginEyeHide.SetActive(!isLoginPasswordVisible);
    }

    public void ToggleRegisterPasswordVisibility()
    {
        isRegisterPasswordVisible = !isRegisterPasswordVisible;
        registerPasswordInput.contentType = isRegisterPasswordVisible ? InputField.ContentType.Standard : InputField.ContentType.Password;
        registerPasswordInput.ForceLabelUpdate();

        registerEyeShow.SetActive(isRegisterPasswordVisible);
        registerEyeHide.SetActive(!isRegisterPasswordVisible);
    }

    public void ToggleRegisterConfirmPasswordVisibility()
    {
        isRegisterConfirmPasswordVisible = !isRegisterConfirmPasswordVisible;
        registerConfirmPasswordInput.contentType = isRegisterConfirmPasswordVisible ? InputField.ContentType.Standard : InputField.ContentType.Password;
        registerConfirmPasswordInput.ForceLabelUpdate();

        registerConfirmEyeShow.SetActive(isRegisterConfirmPasswordVisible);
        registerConfirmEyeHide.SetActive(!isRegisterConfirmPasswordVisible);
    }

    public void SwitchToRegister()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        forgotPasswordPanel.SetActive(false);
        ClearAllInputs();
    }

    public void SwitchToLogin()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        forgotPasswordPanel.SetActive(false);
        ClearAllInputs();
    }

    public void SwitchToForgotPassword()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        forgotPasswordPanel.SetActive(true);
        ClearAllInputs();
    }

    private void ClearAllInputs()
    {
        loginEmailInput.text = "";
        loginPasswordInput.text = "";
        registerEmailInput.text = "";
        registerPasswordInput.text = "";
        registerConfirmPasswordInput.text = "";
        forgotPasswordEmailInput.text = "";

        HidePopup();
    }
}
