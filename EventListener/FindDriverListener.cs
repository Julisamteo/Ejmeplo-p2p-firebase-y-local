using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Google.Maps.Android;
using Firebase.Database;
using UberClone.DataModels;
using UberClone.Helpers;

namespace UberClone.EventListener
{
    class FindDriverListener : Java.Lang.Object, IValueEventListener
    {
        //Events
        public class DriverFoundEventArgs: EventArgs
        {
            public List<AvailableDriver> Drivers { get; set; }
        }

        public event EventHandler<DriverFoundEventArgs> DriversFound;
        public event EventHandler DriverNotFound;

        //Ride Details
        LatLng mPickupLocation;
        string mRideid;
    
        //available drivers
        List<AvailableDriver> availableDrivers = new List<AvailableDriver>();
        public FindDriverListener(LatLng pickupLocation, string rideid)
        {
            mPickupLocation = pickupLocation;
            mRideid = rideid;
        }
        public void OnCancelled(DatabaseError error)
        {
        }

        public void OnDataChange(DataSnapshot snapshot)
        {
            if(snapshot.Value != null)
            {
                var child = snapshot.Children.ToEnumerable<DataSnapshot>();
                availableDrivers.Clear(); 
                foreach(DataSnapshot data in child)
                {
                    if(data.Child("rider_id").Value.ToString() == "waiting")
                    {
                        //Get Driver Location
                        double latitude = double.Parse(data.Child("location").Child("latitude").Value.ToString());          
                        double longitude = double.Parse(data.Child("location").Child("longitude").Value.ToString());
                        LatLng driverLocation = new LatLng(latitude, longitude);

                        AvailableDriver driver = new AvailableDriver();

                        //Compute distance between pickup location and driver location
                        driver.DristanceFromPickup = SphericalUtil.ComputeDistanceBetween(mPickupLocation, driverLocation);
                        driver.ID = data.Key;
                        availableDrivers.Add(driver);

                    }
                }

                if(availableDrivers.Count > 0)
                {
                    availableDrivers = availableDrivers.OrderBy(o => o.DristanceFromPickup).ToList();
                    DriversFound?.Invoke(this, new DriverFoundEventArgs { Drivers = availableDrivers });
                }
                else
                {
                    DriverNotFound.Invoke(this, new EventArgs());
                }
            }
            else
            {
                DriverNotFound.Invoke(this, new EventArgs());
            }
        }

        public void Create()
        {
            FirebaseDatabase database = AppDataHelper.GetDatabase();
            DatabaseReference findDriverRef = database.GetReference("driverAvailable");
            findDriverRef.AddListenerForSingleValueEvent(this);

        }
    }
}