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
using Firebase.Database;
using Java.Util;
using Uber_Rider.DataModels;
using UberClone.DataModels;
using UberClone.Helpers;

namespace UberClone.EventListener
{
    class CreateRequestEventListener : Java.Lang.Object, IValueEventListener
    {
        NewTripDetails newTrip;
        FirebaseDatabase database;
        DatabaseReference newTripRef;
        DatabaseReference notifyDriverRef;

        //Notify Driver
        List<AvailableDriver> mAvailableDrivers;
        AvailableDriver selectDriver;
        //Timer
        System.Timers.Timer RequestTimer = new System.Timers.Timer();
        int TimerCounter = 0;

        //flags
        bool isDriverAccepted;

        //EventHandlers
        public class DriverAcceptedEventArgs: EventArgs
        {
            public AcceptedDriver acceptedDriver { get; set; }
        }
        public class TripUpdateEventArgs : EventArgs
        {
            public LatLng DriverLocation { get; set; }
            public string Status { get; set; }
            public double Fares { get; set; }
        }
        public event EventHandler<DriverAcceptedEventArgs> DriverAccepted;
        public event EventHandler NoDriverAcceptedRequest;
        public event EventHandler<TripUpdateEventArgs> TripUpdates;
        public void OnCancelled(DatabaseError error)
        {

        }

        public void OnDataChange(DataSnapshot snapshot)
        {
            if (snapshot.Value != null)
            {
                if (snapshot.Child("driver_id").Value.ToString() != "waiting")
                {
                    string status = "";
                    double fares = 0;
                    if (!isDriverAccepted)
                    {
                        AcceptedDriver acceptedDriver = new AcceptedDriver();
                        acceptedDriver.ID = snapshot.Child("driver_id").Value.ToString();
                        acceptedDriver.fullname = snapshot.Child("driver_name").Value.ToString();
                        acceptedDriver.phone = snapshot.Child("driver_phone").Value.ToString();
                        isDriverAccepted = true;
                        DriverAccepted.Invoke(this, new DriverAcceptedEventArgs { acceptedDriver = acceptedDriver });
                    }
                    if(snapshot.Child("status").Value != null)
                    {
                        status = snapshot.Child("status").Value.ToString();
                    }
                    if (snapshot.Child("fares").Value.ToString() != null)
                    {
                        fares = double.Parse(snapshot.Child("fares").Value.ToString());
                    }
                    if(isDriverAccepted)
                    {
                        double driverLatitude = double.Parse(snapshot.Child("driver_location").Child("latitude").Value.ToString());
                        double driverLongitude = double.Parse(snapshot.Child("driver_location").Child("longitude").Value.ToString());
                        LatLng driverLocationLatLng = new LatLng(driverLatitude, driverLongitude);
                        TripUpdates.Invoke(this, new TripUpdateEventArgs { DriverLocation = driverLocationLatLng, Status=status, Fares=fares });
                    }
                }
            }
        }

        public  CreateRequestEventListener(NewTripDetails nNewTrip)
        {
            newTrip = nNewTrip;
            
            database = AppDataHelper.GetDatabase();

            RequestTimer.Interval = 1000;
            RequestTimer.Elapsed += RequestTimer_Elapsed;
        }

        private void RequestTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            TimerCounter++;
            
            if(TimerCounter == 30)
            {
                if(!isDriverAccepted)
                { 
                DatabaseReference cancelDriverRef = database.GetReference("driverAvailable/" + selectDriver.ID + "/rider_id");
                cancelDriverRef.SetValue("timeout");
                    if (mAvailableDrivers != null)
                    {
                        NotifyDriver(mAvailableDrivers);
                    }
                    else
                    {
                        RequestTimer.Enabled = true;
                        NoDriverAcceptedRequest?.Invoke(this, new EventArgs());
                    }
                }
            }
        }

        public void CreateRequest()
        {
            newTripRef = database.GetReference("rideRequest").Push();
            HashMap location = new HashMap();
            location.Put("latitude", newTrip.PickupLat);
            location.Put("longitude", newTrip.PickupLng);

            HashMap destination = new HashMap();
            destination.Put("latitude", newTrip.DestinationLat);
            destination.Put("longitude", newTrip.DestinationLng);

            HashMap myTrip = new HashMap();

            newTrip.RideID = newTripRef.Key;
            myTrip.Put("location", location);
            myTrip.Put("destination", destination);
            myTrip.Put("destination_address", newTrip.DestinationAddress);
            myTrip.Put("pickup_address", newTrip.PickupAddress);
            myTrip.Put("rider_id", AppDataHelper.GetCurrentUser().Uid);
            myTrip.Put("payment_method", newTrip.Paymentmethod);
            myTrip.Put("created_at", newTrip.Timestamp.ToString());
            myTrip.Put("driver_id", "waiting");
            myTrip.Put("rider_name", AppDataHelper.GetFullName());
            myTrip.Put("rider_phone", AppDataHelper.GetPhone());


            newTripRef.AddValueEventListener(this);
            newTripRef.SetValue(myTrip);

        }

        public void CancelRequest()
        {
            if(selectDriver != null)
            {
                DatabaseReference cancelDriverRef = database.GetReference("driverAvailable/" + selectDriver.ID + "/rider_id");
                cancelDriverRef.SetValue("cancelled");
            }
            newTripRef.RemoveEventListener(this);
            newTripRef.RemoveValue();
        }

        public void CancelRequestOnTimeOut()
        {
            newTripRef.RemoveEventListener(this);
            newTripRef.RemoveValue();
        }

        public void NotifyDriver(List<AvailableDriver> avaliableDrivers)
        {
            mAvailableDrivers = avaliableDrivers;
            if (mAvailableDrivers.Count >= 1 && mAvailableDrivers != null)
            {
                selectDriver = mAvailableDrivers[0];
                notifyDriverRef = database.GetReference("driverAvailable/" + selectDriver.ID + "/rider_id");
                notifyDriverRef.SetValue(newTrip.RideID);

                if(mAvailableDrivers.Count > 1)
                {
                    mAvailableDrivers.RemoveAt(0);
                }
                else if(mAvailableDrivers.Count == 1)
                {
                    mAvailableDrivers = null;
                }
                RequestTimer.Enabled = true;
               
            }
            else
            {
                RequestTimer.Enabled = true;
                NoDriverAcceptedRequest?.Invoke(this, new EventArgs());
            }
        }

        public void EndTrip()
        {
            newTripRef.RemoveEventListener(this);
            newTripRef = null;
        }
    }
}