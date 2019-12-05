using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using Firebase.Auth;
using Firebase.Database;
using Firebase;
using Android.Gms.Tasks;
using Java.Lang;
using UberClone.EventListener;
using Android.Support.V7.App;
using Java.Util;
using Uber_Rider.Activities;

namespace UberClone.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/UberTheme", MainLauncher = false)]
    public class RegisterActivity : AppCompatActivity
    {
        TextInputLayout fullNameText;
        TextInputLayout phoneText;
        TextInputLayout emailText;
        TextInputLayout passwordText;
        Button registerButton;
        CoordinatorLayout rootView;
        TextView clickToLoginText;

        FirebaseAuth mAuth;
        FirebaseDatabase database;
        TaskCompletionListener TaskCompletionListener = new TaskCompletionListener();
        string fullname, phone, email, password;

        ISharedPreferences preferences = Application.Context.GetSharedPreferences("userinfo", FileCreationMode.Private);
        ISharedPreferencesEditor editor;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.registration);

            InitializeFirebase();
            mAuth = FirebaseAuth.Instance;
            ConnectController();
        }
        void ConnectController()
        {
            fullNameText = FindViewById<TextInputLayout>(Resource.Id.fullNameText);
            phoneText = FindViewById<TextInputLayout>(Resource.Id.phoneText);
            emailText = FindViewById<TextInputLayout>(Resource.Id.emailText);
            passwordText = FindViewById<TextInputLayout>(Resource.Id.passwordText);
            registerButton = FindViewById<Button>(Resource.Id.registerButton);
            rootView = FindViewById<CoordinatorLayout>(Resource.Id.rootView);
            clickToLoginText = FindViewById<TextView>(Resource.Id.clickToLoginText);

            clickToLoginText.Click += ClickToLoginText_Click;
            registerButton.Click += RegisterButton_Click;

        }

        private void ClickToLoginText_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(LoginActivity));
            Finish();
        }

        private void RegisterButton_Click(object sender, EventArgs e)
        {
            fullname = fullNameText.EditText.Text;
            phone = phoneText.EditText.Text;
            email = emailText.EditText.Text;
            password = passwordText.EditText.Text;

            if (fullname.Length < 3)
            {
                Snackbar.Make(rootView, "Please enter a valid name", Snackbar.LengthShort).Show();
                return;
            }
            else if (phone.Length < 9)
            {
                Snackbar.Make(rootView, "Please enter a valid phone", Snackbar.LengthShort).Show();
                return;
            }
            else if (!email.Contains("@"))
            {
                Snackbar.Make(rootView, "Please enter a valid email", Snackbar.LengthShort).Show();
                return;
            }
            else if (password.Length < 6)
            {
                Snackbar.Make(rootView, "Please enter a valid password upto 6 charters", Snackbar.LengthShort).Show();
                return;
            }
            RegisterUser(fullname, phone, email, password);

        }
        void RegisterUser(string name, string phone, string email, string password)
        {


            TaskCompletionListener.Success += TaskCompletionListener_Success;
            TaskCompletionListener.Failure += TaskCompletionListener_Failure;


            mAuth.CreateUserWithEmailAndPassword(email, password)
                .AddOnSuccessListener(this, TaskCompletionListener)
                .AddOnFailureListener(this, TaskCompletionListener);
        }

        private void TaskCompletionListener_Failure(object sender, EventArgs e)
        {
            Snackbar.Make(rootView, "User registration failed", Snackbar.LengthShort).Show();
        }

        private void TaskCompletionListener_Success(object sender, EventArgs e)
        {
            Snackbar.Make(rootView, "User registration was successful", Snackbar.LengthShort).Show();
            HashMap Usermap = new HashMap();
            Usermap.Put("email", email);
            Usermap.Put("name", fullname);
            Usermap.Put("phone", phone);

            DatabaseReference userReference = database.GetReference("users/" + mAuth.CurrentUser.Uid);
            userReference.SetValue(Usermap);
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
                database = FirebaseDatabase.GetInstance(app);
            }
            else
            {
                database = FirebaseDatabase.GetInstance(app);
            }
        }

        void SaveToSharedPreference()
        {

            editor = preferences.Edit();

            editor.PutString("email", email);
            editor.PutString("fullname", fullname);
            editor.PutString("phone", phone);

            editor.Apply();
        }

        //Recuperar dato
        void RetriveDate()
        {
            string email = preferences.GetString("email","");

        }

    }
}