using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UberClone;
using UberClone.Activities;
using UberClone.EventListener;
using Resource = UberClone.Resource;

namespace Uber_Rider.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/UberTheme", MainLauncher = false)]
    public class LoginActivity : AppCompatActivity
    {
        TextInputLayout emailText;
        TextInputLayout passwordText;
        Button loginButton;
        CoordinatorLayout rootView;
        FirebaseAuth mAuth;
        TextView clickToRegisterText;

        TaskCompletionListener TaskCompletionListener = new TaskCompletionListener();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.login);

            emailText = FindViewById<TextInputLayout>(Resource.Id.emailText);
            passwordText = FindViewById<TextInputLayout>(Resource.Id.passwordText);
            loginButton = FindViewById<Button>(Resource.Id.loginButton);
            rootView = FindViewById<CoordinatorLayout>(Resource.Id.rootView);
            clickToRegisterText = FindViewById<TextView>(Resource.Id.clickToRegisterText);

            loginButton.Click += LoginButton_Click;
            clickToRegisterText.Click += ClickToRegisterText_Click;
            InitializeFirebase();

        }

        private void ClickToRegisterText_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(RegisterActivity));
            Finish();
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            string email, password;

            email = emailText.EditText.Text;
            password = passwordText.EditText.Text;

            if (!email.Contains("@"))
            {
                Snackbar.Make(rootView, "Please enter a valid email", Snackbar.LengthShort).Show();
                return;
            }
            else if (password.Length < 6)
            {
                Snackbar.Make(rootView, "Please enter a valid password", Snackbar.LengthShort).Show();
                return;
            }
            LoginUser(email, password);
        }

        private void LoginUser(string email, string password)
        {
            TaskCompletionListener.Success += TaskCompletionListener_Success;
            TaskCompletionListener.Failure += TaskCompletionListener_Failure;

            mAuth.SignInWithEmailAndPassword(email, password)
                .AddOnSuccessListener(this, TaskCompletionListener)
                .AddOnFailureListener(this, TaskCompletionListener);
        }

        private void TaskCompletionListener_Failure(object sender, EventArgs e)
        {
            Snackbar.Make(rootView, "User login failed", Snackbar.LengthShort).Show();
        }

        private void TaskCompletionListener_Success(object sender, EventArgs e)
        {
            StartActivity(typeof(MainActivity));
        }

        void InitializeFirebase()
        {
            var app = FirebaseApp.InitializeApp(this);

            if (app == null)
            {
                var options = new FirebaseOptions.Builder()
                    .SetApplicationId("1:675084165225:android:4d6cc15f480b10eedd82f8")
                    .SetApiKey("AIzaSyCWQNWUfe7mbVSJFP5hmYwcqgbBXUluhQU")
                    .SetDatabaseUrl("https://clone-4ec8d.firebaseio.com")
                    .SetStorageBucket("clone-4ec8d.appspot.com")
                    .SetProjectId("clone-4ec8d")
                    .Build();

                app = FirebaseApp.InitializeApp(this, options);
                mAuth = FirebaseAuth.Instance;
            }
            else
            {
                mAuth = FirebaseAuth.Instance;
            }
        }



    }
}
