using Android;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Com.Google.Maps.Android;
using Java.Util;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Uberclone.Helpers;
using ufinix.Helpers;

namespace Uberclone.Helpers
{
    public class MapFunctionHelper
    {
        string mapKey;
        GoogleMap map;
        public double distance;
        public double duration;
        public string distanceString;
        public string durationString;
        Marker pickupMarker;
        Marker driverLocationMarker;
        bool isRequestionDirection;
        public MapFunctionHelper(string mMapKey, GoogleMap mmap)
        {
            mapKey = mMapKey;
            map = mmap;
        }
        public string GetGeoCodeUrl(string lat, string lng)
        {
            string url = "https://maps.googleapis.com/maps/api/geocode/json?latlng=" + lat + "," + lng + "&key=" + mapKey;
            return url;
        }
        public async Task<string> GetGeoJsonAsync(string url)
        {
            var handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);
            string result = await client.GetStringAsync(url);
            return result;

        }
        public async Task<string> FindCordinateAddress(LatLng position)
        {
            string latitude = position.Latitude.ToString();
            string latitudepunto = latitude.Replace(',', '.');
            string longitude = position.Longitude.ToString();
            string longitudepunto = longitude.Replace(',', '.');
            string url = GetGeoCodeUrl(latitudepunto, longitudepunto);
            string json = "";
            string placeAddress = "";

            json = await GetGeoJsonAsync(url);

            if (!string.IsNullOrEmpty(json))
            {
                var geoCodeData = JsonConvert.DeserializeObject<GeocodingParser>(json);
                placeAddress = geoCodeData.results[0].formatted_address;
                if (geoCodeData.status.Contains("ZERO"))
                {
                    if (geoCodeData.results[0] != null)
                    {
                        placeAddress = geoCodeData.results[0].formatted_address;
                    }
                }
            }
            return placeAddress;
        }
        public async Task<string> GetDirectionJsonAsync(LatLng location, LatLng destination)
        {
            string latitude = location.Latitude.ToString();
            string latitudepunto = latitude.Replace(',', '.');
            string longitude = location.Longitude.ToString();
            string longitudepunto = longitude.Replace(',', '.');
            string latitudeDestination = destination.Latitude.ToString();
            string latitudepuntoDestination = latitudeDestination.Replace(',', '.');
            string longitudeDestination = destination.Longitude.ToString();
            string longitudepuntoDestination = longitudeDestination.Replace(',', '.');
            //origin of route 
            string str_origin = "origin=" + latitudepunto + "," + longitudepunto;

            //destination rout
            string str_destination = "destination=" + latitudepuntoDestination + "," + longitudepuntoDestination;

            //string mode
            string mode = "mode=driving";

            //Building the parameter to the webservices
            string parameters = str_origin + "&" + str_destination + "&" + "&" + mode + "&key=";

            //output 
            string output = "json";

            string key = mapKey;

            //Building the final url string
               
            string url = "https://maps.googleapis.com/maps/api/directions/" + output + "?" + parameters + key;
            Console.WriteLine(url);
            string json = "";
            json = await GetGeoJsonAsync(url);
            return json;
        }

        public void DrawTripOnMap(string json)
        {
            var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);
            var points = directionData.routes[0].overview_polyline.points;
            var line = PolyUtil.Decode(points);

            ArrayList routeList = new ArrayList();
            foreach(LatLng item in line)
            {
                routeList.Add(item);
            }

            PolylineOptions polylineOptions = new PolylineOptions()
                .AddAll(routeList)
                .InvokeWidth(10)
                .InvokeColor(Color.Teal)
                .InvokeStartCap(new SquareCap())
                .InvokeEndCap(new SquareCap())
                .InvokeJointType(JointType.Round)
                .Geodesic(true);

            Android.Gms.Maps.Model.Polyline mpolyLine = map.AddPolyline(polylineOptions);

            //get first point and lastpoint
            LatLng firstPoint = line[0];
            LatLng lastPoint = line[line.Count - 1];

            MarkerOptions pickupMarkerOptions = new MarkerOptions();
            pickupMarkerOptions.SetPosition(firstPoint);
            pickupMarkerOptions.SetTitle("Pickup Location");
            pickupMarkerOptions.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));

            MarkerOptions destinationMarkerOptions = new MarkerOptions();
            destinationMarkerOptions.SetPosition(lastPoint);
            destinationMarkerOptions.SetTitle("Destination");
            destinationMarkerOptions.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed));

            MarkerOptions driverMarkerOptions = new MarkerOptions();
            driverMarkerOptions.SetPosition(firstPoint);
            driverMarkerOptions.SetTitle("current location");
            driverMarkerOptions.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));

            pickupMarker = map.AddMarker(pickupMarkerOptions);
            Marker destinationMarker = map.AddMarker(destinationMarkerOptions);
            driverLocationMarker = map.AddMarker(driverMarkerOptions);

            //get trip bounds
            double southlng = directionData.routes[0].bounds.southwest.lng;
            double southlat = directionData.routes[0].bounds.southwest.lat;
            double northlng = directionData.routes[0].bounds.northeast.lng;
            double northlat = directionData.routes[0].bounds.northeast.lat;

            LatLng southwest = new LatLng(southlat, southlng);
            LatLng northwest = new LatLng(northlat, northlng);
            LatLngBounds tripBound = new LatLngBounds(southwest, northwest);

            map.AnimateCamera(CameraUpdateFactory.NewLatLngBounds(tripBound, 100));
            map.SetPadding(40, 70, 40, 70);
            pickupMarker.ShowInfoWindow();

            duration = directionData.routes[0].legs[0].duration.value;
            distance = directionData.routes[0].legs[0].distance.value;
            durationString = directionData.routes[0].legs[0].duration.text;
            distanceString = directionData.routes[0].legs[0].distance.text;

        }
        public double EstimateFares()
        {
            double basefare = 2500;
            double distancefare = 500;
            double timefare = 100;

            double kmfares = (distance / 1000) * distancefare;
            double minsfares = (duration / 60) * timefare;

            double amount = basefare + kmfares + minsfares ;
            double fares = Math.Floor(amount / 10) * 10;
            return fares;
        }
        public async void UpdateDriverLocationToPickup(LatLng firstPosition, LatLng secondPosition)
        {
            if (!isRequestionDirection)
            {
                isRequestionDirection = true;
                string json = await GetDirectionJsonAsync(firstPosition, secondPosition);
                var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);
                string duration = directionData.routes[0].legs[0].duration.text;
                pickupMarker.Title = "Pickup Location";
                pickupMarker.Snippet = "Your driver is " + duration + " away";
                pickupMarker.ShowInfoWindow();
                isRequestionDirection = false;
            }
        }
        public void UpdateDriverArrived()
        {
            pickupMarker.Title = "Pickup Location";
            pickupMarker.Snippet = "Your driver has arrive";
            pickupMarker.ShowInfoWindow();
        }
        public async void UpdateLocationToDestination(LatLng firstPosition, LatLng secondPosition)
        {
            driverLocationMarker.Visible = true;
            driverLocationMarker.Position = firstPosition;
            map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(firstPosition, 15));

            if (!isRequestionDirection)
            {
                isRequestionDirection = true;
                string json = await GetDirectionJsonAsync(firstPosition, secondPosition);
                var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);
                string duration = directionData.routes[0].legs[0].duration.text;
                driverLocationMarker.Title = "Current Location";
                driverLocationMarker.Snippet = "Your Destination is " + duration + " away";
                driverLocationMarker.ShowInfoWindow();
                isRequestionDirection = false;
            }
        }
    }
}