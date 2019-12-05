using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Firebase;
using Firebase.Database;
using System;
using Android.Views;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android;
using Android.Support.V4.App;
using Android.Content.PM;
using Android.Gms.Location;
using UberClone.Helpers;
using Android.Content;
using Google.Places;
using System.Collections.Generic;
using Uberclone.Helpers;
using Android.Graphics;
using Android.Support.Design.Widget;
using UberClone.EventListener;
using UberClone.Fragments;
using Uber_Rider.DataModels;
using Android.Media;

namespace UberClone
{
    [Activity(Label = "@string/app_name", Theme = "@style/UberTheme", MainLauncher = false)]
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback
    {
        //Firebase

        UserProfileEventListener userProfileEventListener = new UserProfileEventListener();
        CreateRequestEventListener requestListener;
        FindDriverListener FindDriverListener;

        //views
        Android.Support.V7.Widget.Toolbar mainToolbar;
        Android.Support.V4.Widget.DrawerLayout drawerLayout;

        //TextView
        TextView pickupLocationText;
        TextView destiantionLocationText;
        TextView driverNameText;
        TextView tripStatusText;

        //Buttoms
        Button favouritePlacesButton;
        Button locationSetButton;
        Button requestDriverButton;
        RadioButton pickupRadio;
        RadioButton destinationRadio;
        ImageButton callDriverButton;
        ImageButton cancelTripButton;

        //ImageView
        ImageView centerMaker;

        //Layouts
        RelativeLayout layoutPickUp;
        RelativeLayout layoutDestination;

        //BottomSheets
        BottomSheetBehavior tripDetalBottomsheetBehavior;
        BottomSheetBehavior driverAssignedBottomSheetBehaivor;

        GoogleMap mainMap;
        readonly string[] permissionGroupLocation = { Manifest.Permission.AccessCoarseLocation, Manifest.Permission.AccessFineLocation };
        const int requestLocationId = 0;

        LocationRequest mLocationRequest;
        FusedLocationProviderClient locationClient;
        Android.Locations.Location mLastLocation;
        LocationCallbackHelper mLocationCallback;

        static int UPDATE_INTERVAL = 5; //SECONDS
        static int FASTEST_INTERVAL = 5;
        static int DISPLACEMENT = 3; //METERS

        //Helpers
        MapFunctionHelper mapHelper;

        //TripDetail
        LatLng pickupLocationLatLng;
        LatLng destinationLocationLatLng;
        string pickupAddress;
        string destinationAddress;

        //Flaps
        int addressRequest = 1;
        bool takeAddressFromSearch;

        //Fragments 
        RequestDriver requestdriverFragment;

        //DataModels
        NewTripDetails newtripdetails;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            ConnectControl();

            SupportMapFragment mapFragment = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.map);
            mapFragment.GetMapAsync(this);

            CheckLocationPermission();
            CreateLocationRequest();
            GetMyLocation();
            StartLocationUpdates();
            InitializePlaces();
            userProfileEventListener.Create();
        }

        void ConnectControl()
        {
            //DrawerLayout
            drawerLayout = FindViewById<Android.Support.V4.Widget.DrawerLayout>(Resource.Id.drawerLayout);

            //ToolBar
            mainToolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.mainToolbar);
            SetSupportActionBar(mainToolbar);
            SupportActionBar.Title = "";
            Android.Support.V7.App.ActionBar actionBar = SupportActionBar;
            actionBar.SetHomeAsUpIndicator(Resource.Mipmap.ic_menu_action);
            actionBar.SetDisplayHomeAsUpEnabled(true);

            //TextView
            pickupLocationText = FindViewById<TextView>(Resource.Id.pickupLocationText);
            destiantionLocationText = FindViewById<TextView>(Resource.Id.destinationLocationText);
            tripStatusText = FindViewById<TextView>(Resource.Id.tripStatusText);
            driverNameText = FindViewById<TextView>(Resource.Id.driverNameText);

            //Buttom
            favouritePlacesButton = FindViewById<Button>(Resource.Id.favouritePlaceButtom);
            locationSetButton = FindViewById<Button>(Resource.Id.locationSetButton);
            requestDriverButton = FindViewById<Button>(Resource.Id.requestDriverButton);
            pickupRadio = FindViewById<RadioButton>(Resource.Id.pickupRadio);
            destinationRadio = FindViewById<RadioButton>(Resource.Id.destinationpRadio);

            callDriverButton = FindViewById<ImageButton>(Resource.Id.callDriverButton);
            cancelTripButton = FindViewById<ImageButton>(Resource.Id.cancelTripButton);

            requestDriverButton.Click += RequestDriverButton_Click;
            favouritePlacesButton.Click += FavouritePlacesButton_Click;
            locationSetButton.Click += LocationSetButton_Click;
            pickupRadio.Click += PickupRadio_Click;
            destinationRadio.Click += DestinationRadio_Click;

            //ImageView
            centerMaker = FindViewById<ImageView>(Resource.Id.centerMarker);

            //LayoutPickup
            layoutPickUp = FindViewById<RelativeLayout>(Resource.Id.layoutPickUp);
            layoutDestination = FindViewById<RelativeLayout>(Resource.Id.layoutDestination);

            layoutPickUp.Click += LayoutPickUp_Click;
            layoutDestination.Click += LayoutDestination_Click;

            //BottomSheet
            FrameLayout tripDetailsView = FindViewById<FrameLayout>(Resource.Id.tripdetails_bottomsheet);
            FrameLayout rideInfoSheet = FindViewById<FrameLayout>(Resource.Id.bottom_sheet_trip);

            tripDetalBottomsheetBehavior = BottomSheetBehavior.From(tripDetailsView);
            driverAssignedBottomSheetBehaivor = BottomSheetBehavior.From(rideInfoSheet);
        }
        #region TRIP CONFIGURATION
        void TriplocationsSet()
        {
            favouritePlacesButton.Visibility = ViewStates.Invisible;
            locationSetButton.Visibility = ViewStates.Visible; 
        }
        void TripLocationUnset()
        {
            mainMap.Clear();
            layoutPickUp.Clickable = true;
            layoutDestination.Clickable = false;
            layoutPickUp.Clickable = false;
            pickupRadio.Clickable = false;
            destinationRadio.Clickable = false;
            takeAddressFromSearch = true;
            centerMaker.Visibility = ViewStates.Invisible;
            favouritePlacesButton.Visibility = ViewStates.Visible;
            locationSetButton.Visibility = ViewStates.Invisible;
            tripDetalBottomsheetBehavior.State = BottomSheetBehavior.StateHidden;
        }

        void TripDrawOnMap()
        {
            layoutDestination.Clickable = false;
            layoutPickUp.Clickable = false;
            pickupRadio.Clickable = false;
            destinationRadio.Clickable = false;
            takeAddressFromSearch = true;
            centerMaker.Visibility = ViewStates.Invisible;
        }
        #endregion

        #region CLICK EVENT HANDLERS

        private void RequestDriverButton_Click(object sender, EventArgs e)
        {
            requestdriverFragment = new RequestDriver(mapHelper.EstimateFares());
            requestdriverFragment.Cancelable = false;
            var trans = SupportFragmentManager.BeginTransaction();
            requestdriverFragment.Show(trans, "Request");
            requestdriverFragment.CancelRequest += RequestdriverFragment_CancelRequest;

            newtripdetails = new NewTripDetails();
            newtripdetails.DestinationAddress = destinationAddress;
            newtripdetails.PickupAddress = pickupAddress;
            newtripdetails.DestinationLat = destinationLocationLatLng.Latitude;
            newtripdetails.DestinationLng = destinationLocationLatLng.Longitude;
            newtripdetails.DistanceString = mapHelper.distanceString;
            newtripdetails.DistanceValue = mapHelper.distance;
            newtripdetails.DurationString = mapHelper.durationString;
            newtripdetails.DurationValue = mapHelper.duration;
            newtripdetails.EstimateFare = mapHelper.EstimateFares();
            newtripdetails.Paymentmethod = "cash";
            newtripdetails.PickupLat = pickupLocationLatLng.Latitude;
            newtripdetails.PickupLng = pickupLocationLatLng.Longitude;
            newtripdetails.Timestamp = DateTime.Now;


            requestListener = new CreateRequestEventListener(newtripdetails);
            requestListener.NoDriverAcceptedRequest += RequestListener_NoDriverAcceptedRequest;
            requestListener.DriverAccepted += RequestListener_DriverAccepted;
            requestListener.TripUpdates += RequestListener_TripUpdates;
            requestListener.CreateRequest();

            FindDriverListener = new FindDriverListener(pickupLocationLatLng, newtripdetails.RideID);
            FindDriverListener.DriversFound += FindDriverListener_DriversFound;
            FindDriverListener.DriverNotFound += FindDriverListener_DriverNotFound;
            FindDriverListener.Create();

        }

        private void RequestListener_TripUpdates(object sender, CreateRequestEventListener.TripUpdateEventArgs e)
        {
            if(e.Status == "acepted")
            {
                tripStatusText.Text = "Coming";
                mapHelper.UpdateDriverLocationToPickup(pickupLocationLatLng, e.DriverLocation);
            }
            else if(e.Status == "arrived")
            {
                tripStatusText.Text = "Driver Arrived";
                mapHelper.UpdateDriverArrived();
                MediaPlayer player = MediaPlayer.Create(this, Resource.Raw.alert);
                player.Start();
            }
            else if(e.Status == "ontrip")
            {
                tripStatusText.Text = "On Trip";
                mapHelper.UpdateLocationToDestination(e.DriverLocation, destinationLocationLatLng);
            }
            else if(e.Status == "ended")
            {
                requestListener.EndTrip();
                requestListener = null;
                TripLocationUnset();
                driverAssignedBottomSheetBehaivor.State = BottomSheetBehavior.StateHidden;


                MakePaymentFragment makePaymentFragment = new MakePaymentFragment(e.Fares);
                makePaymentFragment.Cancelable = false;
                var trans = SupportFragmentManager.BeginTransaction();
                makePaymentFragment.Show(trans, "payment");
                makePaymentFragment.PaymentComplete += (i, p) =>
                {
                    makePaymentFragment.Dismiss();
                };
            }
        }

        private void RequestListener_DriverAccepted(object sender, CreateRequestEventListener.DriverAcceptedEventArgs e)
        {
            if(requestdriverFragment != null)
            {
                requestdriverFragment.Dismiss();
                requestdriverFragment = null;
            }
            driverNameText.Text = e.acceptedDriver.fullname;
            tripStatusText.Text = "Coming";

            tripDetalBottomsheetBehavior.State = BottomSheetBehavior.StateHidden;
            driverAssignedBottomSheetBehaivor.State = BottomSheetBehavior.StateExpanded;
        }

        private void RequestListener_NoDriverAcceptedRequest(object sender, EventArgs e)
        {
            RunOnUiThread(()=>{
                if (requestdriverFragment != null && requestListener != null)
                {
                    requestListener.CancelRequestOnTimeOut();
                    requestListener = null;
                    requestdriverFragment.Dismiss();
                    requestdriverFragment = null;

                    Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                    alert.SetTitle("Message");
                    alert.SetMessage("Avaliable drivers couldn't acecept your ride request, try again in a few moments");
                    alert.Show();
                }
            });
           
        }

        private void FindDriverListener_DriverNotFound(object sender, EventArgs e)
        {
            if(requestdriverFragment != null && requestListener != null)
            {
                requestListener.CancelRequest();
                requestListener = null;
                requestdriverFragment.Dismiss();
                requestdriverFragment = null;

                Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Message");
                alert.SetMessage("No avaliable driver found, try again in a few moments");
                alert.Show();
            }
        }

        private void FindDriverListener_DriversFound(object sender, FindDriverListener.DriverFoundEventArgs e)
        {
            if(requestListener != null)
            {
                requestListener.NotifyDriver(e.Drivers);
            }
        }

        private void RequestdriverFragment_CancelRequest(object sender, EventArgs e)
        {
           if(requestdriverFragment != null && requestListener != null)
            {
                requestListener.CancelRequest();
                requestListener = null;
                requestdriverFragment.Dismiss();
                requestdriverFragment = null;
            }
        }

        async void LocationSetButton_Click(object sender, EventArgs e)
        {
            locationSetButton.Text = "Please wait...";
            locationSetButton.Enabled = false;
            string json;
            json = await mapHelper.GetDirectionJsonAsync(pickupLocationLatLng, destinationLocationLatLng);

            if (!string.IsNullOrEmpty(json))
            {
                TextView txtFare = FindViewById<TextView>(Resource.Id.tripEstimateFareText);
                TextView txtTime = FindViewById<TextView>(Resource.Id.newTripTimeText);


                mapHelper.DrawTripOnMap(json);

                txtFare.Text = "$" + mapHelper.EstimateFares().ToString() + " - " + (mapHelper.EstimateFares() + 2000).ToString();
                txtTime.Text = mapHelper.durationString;
                tripDetalBottomsheetBehavior.State = BottomSheetBehavior.StateExpanded;
                TripDrawOnMap();
            }
            locationSetButton.Text = "Done";
            locationSetButton.Enabled = true;
        }


        private void FavouritePlacesButton_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void PickupRadio_Click(object sender, EventArgs e)
        {
            addressRequest = 1;
            pickupRadio.Checked = true;
            destinationRadio.Checked = false;
            takeAddressFromSearch = false;
            centerMaker.SetColorFilter(Color.DarkGreen);
        }

        private void DestinationRadio_Click(object sender, EventArgs e)
        {
            addressRequest = 2;
            pickupRadio.Checked = false;
            destinationRadio.Checked = true;
            takeAddressFromSearch = false;
            centerMaker.SetColorFilter(Color.Red);


        }


        void InitializePlaces()
        {
            string mapKey = Resources.GetString(Resource.String.mapKey);
            if(!PlacesApi.IsInitialized)
            {
                PlacesApi.Initialize(this, mapKey);
            }
        }
        private void LayoutDestination_Click(object sender, EventArgs e)
        {
            //Intent intent = new PlaceAutocomplete.IntentBuilder(PlaceAutocomplete.ModeOverlay)
            //.Build(this);
            //StartActivityForResult(intent, 2);

            List<Place.Field> fields = new List<Place.Field>();
            fields.Add(Place.Field.Id);
            fields.Add(Place.Field.Name);
            fields.Add(Place.Field.LatLng);
            fields.Add(Place.Field.Address);

            Intent intent = new Autocomplete.IntentBuilder(AutocompleteActivityMode.Overlay, fields)
                .SetCountry("CO")
                .Build(this);
            StartActivityForResult(intent, 2);

        }

        private void LayoutPickUp_Click(object sender, EventArgs e)
        {
            //Intent intent = new PlaceAutocomplete.IntentBuilder(PlaceAutocomplete.ModeOverlay)
            //.Build(this);
            //StartActivityForResult(intent, 1);

            List<Place.Field> fields = new List<Place.Field>();
            fields.Add(Place.Field.Id);
            fields.Add(Place.Field.Name);
            fields.Add(Place.Field.LatLng);
            fields.Add(Place.Field.Address);

            Intent intent = new Autocomplete.IntentBuilder(AutocompleteActivityMode.Overlay, fields)
                .SetCountry("CO")
                .Build(this);
            StartActivityForResult(intent, 1);
        }
        #endregion

        #region MAP AND LOCATION SERVICES

        public void OnMapReady(GoogleMap googleMap)
        {
            try
            {
                bool success = googleMap.SetMapStyle(MapStyleOptions.LoadRawResourceStyle(this, Resource.Raw.silvermapstyle));
            }
            catch
            {

            }            
            mainMap = googleMap;
            mainMap.CameraIdle += MainMap_CameraIdle;
            string mapKey = Resources.GetString(Resource.String.mapKey);
            mapHelper = new MapFunctionHelper(mapKey,mainMap);
        }

        async private void MainMap_CameraIdle(object sender, EventArgs e)
        {
            if (!takeAddressFromSearch)
            {
                if (addressRequest == 1)
                {
                    pickupLocationLatLng = mainMap.CameraPosition.Target;
                    pickupAddress = await mapHelper.FindCordinateAddress(pickupLocationLatLng);
                    pickupLocationText.Text = pickupAddress;
                }
                else if (addressRequest == 2)
                {
                    destinationLocationLatLng = mainMap.CameraPosition.Target;
                    destinationAddress = await mapHelper.FindCordinateAddress(destinationLocationLatLng);
                    destiantionLocationText.Text = destinationAddress;
                    TriplocationsSet();

                }
            }

        }

        bool CheckLocationPermission()
        {
            bool permissionGranted = false;

            if(ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation ) != Android.Content.PM.Permission.Granted &&
                ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Android.Content.PM.Permission.Granted)
            {
                permissionGranted = false;
                RequestPermissions(permissionGroupLocation, requestLocationId);
            }
            else
            {
                permissionGranted = true;

            }
            return permissionGranted;
        }

        void CreateLocationRequest()
        {
            mLocationRequest = new LocationRequest();
            mLocationRequest.SetInterval(UPDATE_INTERVAL);
            mLocationRequest.SetFastestInterval(FASTEST_INTERVAL);
            mLocationRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
            mLocationRequest.SetSmallestDisplacement(DISPLACEMENT);
            locationClient = LocationServices.GetFusedLocationProviderClient(this);
            mLocationCallback = new LocationCallbackHelper();
            mLocationCallback.myLocation += mLocationCallback_myLocation;

        }

        private void mLocationCallback_myLocation(object sender, LocationCallbackHelper.OnLocationCapturedEventArgs e)
        {
            mLastLocation = e.Location;
            LatLng myPosition = new LatLng(mLastLocation.Latitude, mLastLocation.Longitude);
            mainMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(myPosition, 17));

        }

        void StartLocationUpdates() 
        {
            if (CheckLocationPermission())
            {
                locationClient.RequestLocationUpdates(mLocationRequest, mLocationCallback, null);
            }
        }
        void StopLocationUpdates()
        {
            if(locationClient != null && mLocationCallback != null)
            {
                locationClient.RemoveLocationUpdates(mLocationCallback);
            }
        }

        async void GetMyLocation()
        {
            if (!CheckLocationPermission())
            {
                return;
            }

            mLastLocation = await locationClient.GetLastLocationAsync();

            if (mLastLocation != null) 
            {
                LatLng myPosition = new LatLng(mLastLocation.Latitude, mLastLocation.Longitude);
                mainMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(myPosition, 17));
            }

        }
        #endregion

        #region OVERRIDE METHODS

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    drawerLayout.OpenDrawer((int)GravityFlags.Left);
                    return true;

                default:
                    return base.OnOptionsItemSelected(item);

            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(grantResults.Length > 1 )
            {
                return;
            }
            if (grantResults.Length < 1)
            {
                StartLocationUpdates();
            }
            else
            {
                Toast.MakeText(this, "Permission was denied", ToastLength.Short).Show();
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Android.App.Result resultCode, Intent data)
        {
            if(requestCode == 1)
            {
                if(resultCode == Android.App.Result.Ok)
                {
                    takeAddressFromSearch = true;
                    pickupRadio.Checked = false;
                    destinationRadio.Checked = false;
                    var place = Autocomplete.GetPlaceFromIntent(data);
                    pickupLocationText.Text = place.Name.ToString();
                    pickupLocationLatLng = place.LatLng;
                    pickupAddress = place.Name.ToString();
                    mainMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(place.LatLng, 15));
                    centerMaker.SetColorFilter(Color.DarkGreen);

                }
            }
            if(requestCode == 2)
            {
                if (resultCode == Android.App.Result.Ok)
                {
                    takeAddressFromSearch = true;
                    pickupRadio.Checked = false;
                    destinationRadio.Checked = false;
                    var place = Autocomplete.GetPlaceFromIntent(data);
                    destiantionLocationText.Text = place.Name.ToString();
                    destinationLocationLatLng = place.LatLng;
                    destinationAddress = place.Name.ToString();
                    mainMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(place.LatLng, 15));
                    centerMaker.SetColorFilter(Color.Red);
                    TriplocationsSet();

                }
            }
        }
        #endregion
    }
}