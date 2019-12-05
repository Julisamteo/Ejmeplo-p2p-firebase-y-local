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

namespace UberClone.DataModels
{
    class AvailableDriver
    {
        public string ID { get; set; }
        public double DristanceFromPickup { get; set; }
    }
}