{
    let serverBusy = false;
    let map;
    let markedFlight;
    let flightPath;

    // Tables
    let intTable = document.getElementById("flightTable");
    let intRows = intTable.getElementsByTagName("tr");
    let extTable = document.getElementById("extFlightTable");
    let extRows = extTable.getElementsByTagName("tr");

    // Flight class
    class flight {
        constructor(flightId, longitude, latitude, passengers,
            company_Name, dateTime, isExternal) {
            this.flightId = flightId;
            this.longitude = longitude;
            this.latitude = latitude;
            this.passengers = passengers;
            this.company_Name = company_Name;
            this.dateTime = dateTime;
            this.isExternal = isExternal;
        }
    }

    // Map pin class
    class mark {
        constructor(pin, id) {
            this.pin = pin;
            this.id = id;
        }
    }

    // Flights set
    let flightsSet = new Set();
    // Markers set
    let markersSet = new Set();

    // Drag and Drop Zone
    let pictureOn = false;
    let img = document.createElement("img");
    let src = document.getElementById("dragZone");
    let tableHeaders = document.getElementById("flightTableHeader");
    let myFlightHeader = document.getElementById("myFlightsHeader");
    img.src = "img/dad_img.png";
    img.style.height = "200px";
    img.style.width = "400px";

    if (window.File && window.FileReader && window.FileList && window.Blob) {
        // Setup the d&d listeners.
        let dropZone = document.getElementById("dragZone");
        dropZone.addEventListener("dragover", handleDragOver, false);
        dropZone.addEventListener("dragleave", handleDragLeave, false);
        dropZone.addEventListener("drop", handleJSONDrop, false);
    } else {
        showError("The File APIs are not fully supported in this browser.");
    }

    // Add time to date (by seconds)
    Date.prototype.addSeconds = function (s) {
        this.setSeconds(this.getSeconds() + s);
        return this;
    };

    // Write the flight details and draw its path when clicked
    let flightClicked = function (id) {
        markedFlight = id;
        let xhttp = new XMLHttpRequest();
        // server respose
        xhttp.onloadend = function () {
            serverBusy = false;
            if (this.readyState == 4 && this.status == 200) {
                try {
                    markFlight(this.responseText);
                } catch (error) {
                    showError("Getting bad flight plan files from server");
                }
            }
            else {
                showError(xhttp.response);
            }
        };
        serverBusy = true;
        // generate and send the request to the server of getting flight plan by id
        xhttp.open("GET", "/api/FlightPlan/".concat(id), true);
        xhttp.send();
    }

    // When drop files on the drop zone
    function handleJSONDrop(evt) {
        pictureOn = false;
        // append tables and remove image
        src.appendChild(myFlightHeader);
        src.appendChild(tableHeaders);
        src.appendChild(intTable);
        src.removeChild(img);
        evt.stopPropagation();
        evt.preventDefault();
        let files = evt.dataTransfer.files;
        // Loop through the FileList and read
        for (let i = 1, f = files[0]; f; i++) {
            // Only process json files.
            if (!f.type.match("application/json")) {
                showError("Can not load ".concat(f.name)
                    .concat(", please upload .json files only"));
                continue;
            }
            // Generate and send request to server to load flight plan
            let xhr = new XMLHttpRequest();
            xhr.onloadend = function () {
                if (this.status != 200)
                    showError(xhr.response);
                serverBusy = false;
            }
            xhr.open("POST", "/api/FlightPlan", true);
            serverBusy = true;
            xhr.send(f);
            f = files[i];
        }
    }
    function showError(msg) {
        document.getElementById("message").style.display = "block";
        document.getElementById("message").innerHTML = msg;
        setTimeout(function () {
            document.getElementById("message").style.display = "none";
        }, 5000);
    }

    // When drag leave drag zone - remove the images and represent the tables
    function handleDragLeave() {
        if (pictureOn === true) {
            pictureOn = false;
            src.removeChild(img);
            src.appendChild(myFlightHeader);
            src.appendChild(tableHeaders);
            src.appendChild(intTable);

        }
    }

    // When file drag over drag zone - remove the tables and present the image
    function handleDragOver(evt) {
        if (pictureOn === false) {
            pictureOn = true;
            src.removeChild(tableHeaders);
            src.removeChild(intTable);
            src.removeChild(myFlightHeader);
            src.appendChild(img);
        }
        evt.stopPropagation();
        evt.preventDefault();
        evt.dataTransfer.dropEffect = "copy";
    }


    // Clear map and tables and clear details section and arrays
    function clearAll() {
        clearDetails();
        intTable.innerHTML = "";
        extTable.innerHTML = "";
        for (let marker of markersSet) {
            marker.pin.setMap(null);
        }
        if (flightPath != null) {
            flightPath.setMap(null);
        }
        markersSet.clear();
        flightsSet.clear();
    }

    // Clear all marks from map and tables and clear flight-details section
    function unmarkAll() {
        clearDetails();
        if (flightPath != null)
            flightPath.setMap(null);
        for (let marker of markersSet) {
            marker.pin.setIcon("img/inactive_plane_icon.png");
        }
        for (let j = 0; j < intRows.length; j++) {
            intTable.rows[j].style.backgroundColor = "White";
        }
        for (let j = 0; j < extRows.length; j++) {
            extTable.rows[j].style.backgroundColor = "White";
        }
    }

    // Initialize map
    function initMap() {
        let options = {
            zoom: 3,
            center: { lat: 32.3232919, lng: 34.85538661 },
        }
        map = new google.maps.Map(document.getElementById("map"), options);
        // When click on map - unmark all components
        map.addListener("click", unmarkAll);
    }

    // Clear rows of flights that no longer send from server and insert new flights
    function updateLists() {
        // Internal Table
        for (let j = 0; j < intRows.length; j++) {
            if (!flightExist($("td:first", intTable.rows[j]).text())) {
                intTable.deleteRow(j);
            }
        }
        // External Table
        for (let j = 0; j < extRows.length; j++) {
            if (!flightExist($("td:first", extTable.rows[j]).text())) {
                extTable.deleteRow(j);
            }
        }
        insertNewFlightsToLists();
        addRowHandlers();
    }
    // Check if flight exists in the array by id
    function flightExist(id) {
        for (let flight of flightsSet) {
            if (flight.flightId === id) {
                return true;
            }
        }
        return false;
    }

    // Insert flights to their table (external and internal tables)
    function insertNewFlightsToLists() {
        let table;
        for (let flight of flightsSet) {
            if (inTable(flight.flightId)) {
                continue;
            }
            if (flight.isExternal) {
                table = extTable;
            } else {
                table = intTable;
            }
            // Create an empty <tr> element and add it to the 1st
            // position of the table:
            let row = table.insertRow(0);
            // Insert new cells (<td> elements) at the 1st and 2nd position of the
            // "new" < tr > element:
            let cell1 = row.insertCell(0);
            cell1.style.width = "50%";
            let cell2 = row.insertCell(1);
            cell2.style.width = "40%";
            if (!flight.isExternal) {
                // If the flight is internal - add a delete cell to the tabke
                let cell3 = row.insertCell(2);
                cell3.style.width = "10px";
                let trashIcon = document.createElement("img");
                trashIcon.src = "img/trash_icon.png";
                cell3.appendChild(trashIcon);
                cell3.addEventListener("click", function () {
                    // Create and send a request for server to delete this flight
                    let currentRow = $(this).closest("tr");
                    let id = currentRow[0].cells[0].textContent;
                    let xhttp = new XMLHttpRequest();
                    xhttp.onloadend = function () {
                        if (this.status != 200)
                            showError(xhttp.response);
                        serverBusy = false;
                    }
                    xhttp.open("DELETE", "/api/Flights/".concat(id), true);
                    serverBusy = true;
                    xhttp.send();
                    if (id === markedFlight)
                        unmarkAll();
                });
            }
            // Add deatils text to the new cells:
            cell1.innerHTML = flight.flightId;
            cell2.innerHTML = flight.company_Name;
        }
    }

    // Check if a flight is inside internal flight table or external flight table
    function inTable(id) {
        for (let j = 0; j < intRows.length; j++) {
            if (id === $("td:first", intTable.rows[j]).text()) {
                return true;
            }
        }

        for (let j = 0; j < extRows.length; j++) {
            if (id === $("td:first", extTable.rows[j]).text()) {
                return true;
            }
        }
        return false;
    }

    // Add pin to a new flights, clear pins of old flight,
    // and change position of existing filghts
    function updateMap() {
        // Clear old pins
        for (let marker of markersSet) {
            if (!flightExist(marker.id)) {
                if (marker.id === markedFlight) {
                    clearDetails();
                    flightPath.setMap(null);
                }
                marker.pin.setMap(null);
                markersSet.delete(marker);
            }
        }
        // For all flights
        for (let flight of flightsSet) {
            let marker = getIdPin(flight.flightId);
            // If a new flight - add a pin
            if (marker == null) {
                marker = new google.maps.Marker({
                    position: { lat: flight.latitude, lng: flight.longitude },
                    map: map,
                    animation: google.maps.Animation.DROP,
                    icon: "img/inactive_plane_icon.png"
                });
                // Add click listener to the markers
                marker.addListener("click", pinClicked)
                markersSet.add(new mark(marker, flight.flightId));
            }
            else {
                // If a old flight - Change pin position
                marker.setPosition(new google.maps.LatLng(flight.latitude,
                    flight.longitude));
            }
        }
    }

    function pinClicked() {
        let id;
        // Unmark all screen markers
        unmarkAll();
        // Set the marker icon to active icon
        this.setIcon("img/active_plane_icon.png");
        // Find the marker flight id
        for (let marker of markersSet)
            if (this == marker.pin) {
                id = marker.id;
                break;
            }
        // Search the line with the same id in the flight tables and mark it
        let found = 0;
        for (let j = 0; j < intRows.length; j++) {
            if (id === $("td:first", intTable.rows[j]).text()) {
                intTable.rows[j].style.backgroundColor = "87CEFA";
                found = 1;
                break;
            }
        }
        if (found == 0) {
            for (let j = 0; j < extRows.length; j++) {
                if (id === $("td:first", extTable.rows[j]).text()) {
                    extTable.rows[j].style.backgroundColor = "87CEFA";
                    break;
                }
            }
        }
        flightClicked(id);
    }

    // clear flight details section
    function clearDetails() {
        document.getElementById("companyId").innerHTML = "";
        document.getElementById("takeId").innerHTML = "";
        document.getElementById("landingId").innerHTML = "";
        document.getElementById("fromId").innerHTML = "";
        document.getElementById("toId").innerHTML = "";
        document.getElementById("passId").innerHTML = "";
    }

    // Check if a flight have a pin on the map (by id)
    function getIdPin(id) {
        for (let marker of markersSet) {
            if (marker.id === id)
                return marker.pin;
        }
        return null;
    }

    // Add event handle to row click
    function addRowHandlers() {
        // Assign the event handler for each row in the tables
        for (let i = 0; i < intRows.length; i++) {
            let currentRow = intTable.rows[i];
            setRowEvents(currentRow);
        }
        for (let i = 0; i < extRows.length; i++) {
            let currentRow = extTable.rows[i];
            setRowEvents(currentRow);
        }
    }
    // Get a row and set some events handler
    function setRowEvents(currentRow) {
        currentRow.onmouseover = onMouseOver;
        currentRow.onmouseout = onMouseout;
        currentRow.cells[0].onclick = createClickHandler(currentRow);
        currentRow.cells[1].onclick = createClickHandler(currentRow);
    }
    // Add mouse over event
    function onMouseOver() {
        document.body.style.cursor = "pointer";
        this.style.fontWeight = "bold";
    }
    // Add mouse out event
    function onMouseout() {
        document.body.style.cursor = "default";
        this.style.fontWeight = "normal";
    }
    // Add click handler for the first 2 cells
    function createClickHandler(row) {
        return function () {
            let rowId;
            unmarkAll();
            // Mark the appropriate pin
            for (let marker of markersSet) {
                rowId = $("td:first", row).text();
                if (marker.id === rowId) {
                    map.panTo(marker.pin.position);
                    marker.pin.setIcon("img/active_plane_icon.png");
                } else {
                    marker.pin.setIcon("img/inactive_plane_icon.png");
                }
            }
            // Mark the appropriate row
            row.style.backgroundColor = "#87CEFA";
            flightClicked(rowId);
        };
    }

    function markFlight(responseText) {
        // read server response and present it in flight
        // details section and draw the path on the map
        let obj = JSON.parse(responseText);
        let fromLat = obj.initial_location.latitude
            .toString().substring(0, 5);
        let fromLng = obj.initial_location.longitude
            .toString().substring(0, 5);
        let toLat = obj.segments[obj.segments.length - 1]
            .latitude.toString().substring(0, 5);
        let toLng = obj.segments[obj.segments.length - 1]
            .longitude.toString().substring(0, 5);
        let flightTime = 0;
        let flightPlanCoordinates = [];
        flightPlanCoordinates[0] = {
            lat: obj.initial_location.latitude,
            lng: obj.initial_location.longitude
        };
        for (let i = 0; i < obj.segments.length; i++) {
            if (obj.segments[i].latitude == null ||
                obj.segments[i].latitude == null)
                throw "";
            flightPlanCoordinates[i + 1] = {
                lat: obj.segments[i].latitude,
                lng: obj.segments[i].longitude
            };
            flightTime += parseFloat(obj.segments[i].timespan_seconds);
        }
        flightPath = new google.maps.Polyline({
            path: flightPlanCoordinates,
            geodesic: true,
            strokeColor: "#00FF00",
            strokeOpacity: 1.0,
            strokeWeight: 2
        });
        flightPath.setMap(map);
        // Write the flight fetails
        document.getElementById("companyId").innerHTML =
            obj.company_name.toString();
        document.getElementById("takeId").innerHTML = new Date(obj
            .initial_location.date_time)
            .toISOString().split(".")[0] + "Z";
        document.getElementById("landingId").innerHTML = new Date(obj
            .initial_location.date_time)
            .addSeconds(flightTime).toISOString().split(".")[0] + "Z";
        document.getElementById("fromId").innerHTML = "lat: "
            .concat(fromLat).concat(", ").concat("lng: ").concat(fromLng);
        document.getElementById("toId").innerHTML = "lat: "
            .concat(toLat).concat(", ").concat("lng: ").concat(toLng);
        document.getElementById("passId").innerHTML = obj.passengers.toString();
    }

    // Ask the server for all the active flights relative to the currect time
    function getFlights() {
        let counter = 0;
        let xhttp = new XMLHttpRequest();
        let currentTime;
        // On server response
        xhttp.onloadend = function () {
            serverBusy = false;
            if (this.readyState == 4 && this.status == 200) {
                if (this.responseText == null) {
                    clearAll();
                }
                else {
                    flightsSet.clear();
                    const words = this.responseText.replace("]", "")
                        .replace("[", "")
                        .concat(",").split("},");
                    for (let i = 0; i < words.length - 1; i++) {
                        let obj = JSON.parse(words[i].concat("}"));
                        try {
                            if (obj.is_external == null)
                                throw "";
                            flightsSet.add(new flight(obj.flight_id.toString(),
                                obj.longitude.toString(), obj.latitude.toString(),
                                obj.passengers.toString(),
                                obj.company_name.toString(),
                                obj.date_time.toString(), obj.is_external));
                        }
                        catch (error) {
                            showError("getting bad flight files from server");
                        }
                    }
                    updateMap();
                    updateLists();
                }
            }
        };
        setInterval(function () {
            if (!serverBusy) {
                counter = 0;
                serverBusy = true;
                // Generate and send a request for flights
                showTime();
                currentTime = new Date(new Date().toString())
                    .toISOString().split(".")[0] + "Z";
                xhttp.open("GET", "/api/Flights?relative_to="
                    .concat(currentTime).concat("&sync_all"), true);
                xhttp.send();
            } else if (counter == 10) {
                counter = 0;
                showError("server lagging, maybe try to restart the server");
            }
            else {
                counter++;
            }
        }, 1000);
    }
    function showTime() {
        const dateAndTime = new Date().toISOString().split("T");
        const time = dateAndTime[1].split(":");
        document.getElementById("timeHeader").innerHTML =
            time[0] + ":" + time[1] + "<br>" + dateAndTime[0];
    }

    // Ask the server for all the relative flights
    showTime();
    getFlights();
}