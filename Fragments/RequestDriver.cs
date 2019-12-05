using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace UberClone.Fragments
{
    public class RequestDriver : Android.Support.V4.App.DialogFragment
    {
        public event EventHandler CancelRequest;

        double mfares;
        Button cancelRequestButton;
        TextView txtFares;
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            View view =  inflater.Inflate(Resource.Layout.request_driver, container, false);
            cancelRequestButton = view.FindViewById<Button>(Resource.Id.cancelrequestButton);
            cancelRequestButton.Click += CancelRequestButton_Click;
            txtFares = view.FindViewById<TextView>(Resource.Id.faresText);
            txtFares.Text = "$" + mfares.ToString();
            return view;
        }

        private void CancelRequestButton_Click(object sender, EventArgs e)
        {
            CancelRequest?.Invoke(this, new EventArgs());
        }

        public RequestDriver(double fares)
        {
            mfares = fares;
        }
    }
}