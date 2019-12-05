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
    public class MakePaymentFragment : Android.Support.V4.App.DialogFragment
    {

        double mfares;
        TextView TotalFaresText;
        Button makePaymentButton;

        public EventHandler PaymentComplete;
        public MakePaymentFragment(double fares)
        {
            mfares = fares;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.makepayment, container, false);
            TotalFaresText = view.FindViewById<TextView>(Resource.Id.totalfaresText);
            makePaymentButton = view.FindViewById<Button>(Resource.Id.makePaymentButton);

            TotalFaresText.Text = "$" + mfares.ToString();
            makePaymentButton.Click += MakePaymentButton_Click;

            return view;
        }

        private void MakePaymentButton_Click(object sender, EventArgs e)
        {
            PaymentComplete.Invoke(this, new EventArgs());
        }
    }
}