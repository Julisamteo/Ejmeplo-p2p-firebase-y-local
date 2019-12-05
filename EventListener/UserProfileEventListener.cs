using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase.Database;
using UberClone.Helpers;

namespace UberClone.EventListener
{
    class UserProfileEventListener : Java.Lang.Object, IValueEventListener
    {
        ISharedPreferences preferences = Application.Context.GetSharedPreferences("userinfo", FileCreationMode.Private);
        ISharedPreferencesEditor editor;
        public void OnCancelled(DatabaseError error)
        {
            throw new NotImplementedException();
        }

        public void OnDataChange(DataSnapshot snapshot)
        {
            if(snapshot.Value != null)
            {
                string fullname, email, phone ="";
                fullname = (snapshot.Child("name") != null) ? snapshot.Child("name").Value.ToString() : "";
                email = (snapshot.Child("email") != null) ? snapshot.Child("email").Value.ToString() : "";
                if (snapshot.Child("phone") != null)
                {
                    phone = snapshot.Child("phone").Value.ToString();
                }
                editor.PutString("name", fullname);
                editor.PutString("email", email);
                editor.PutString("phone", phone);
                editor.Apply();
            }
        }
        public void Create()
        {
            editor = preferences.Edit();
            FirebaseDatabase database = AppDataHelper.GetDatabase();
            string userId = AppDataHelper.GetCurrentUser().Uid;
            DatabaseReference profileReference = database.GetReference("users/" + userId);
            profileReference.AddValueEventListener(this);
        }
    }
}