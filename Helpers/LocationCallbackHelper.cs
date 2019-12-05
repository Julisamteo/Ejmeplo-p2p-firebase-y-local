using Android.Gms.Location;
using Android.Util;
using System;
using EventArgs = System.EventArgs;

namespace UberClone.Helpers
{
    public class LocationCallbackHelper :  LocationCallback
    {
        public EventHandler<OnLocationCapturedEventArgs> myLocation;
        public class OnLocationCapturedEventArgs : EventArgs
        {
            public Android.Locations.Location Location { get; set; }
        }

        public override void OnLocationAvailability(LocationAvailability locationAvailability)
        {
            Log.Debug("Uber_Clone", "IsLocationAvaliable: {0}", locationAvailability.IsLocationAvailable);
        }

        public override void OnLocationResult(LocationResult result)
        {
            if(result.Locations.Count != 0)
            {
                myLocation?.Invoke(this, new OnLocationCapturedEventArgs { Location = result.Locations[0] });
            }
        }

    }
}