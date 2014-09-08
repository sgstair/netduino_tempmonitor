netduino_tempmonitor
====================

Basic temperature recording system using Netduino

I've wanted one of these for a long time, so I built one.

Overview and caveats:
* Netduino plus 2 reads up to 32 DS18B20 sensors and sends the data to a webservice
* Webservice inserts data into a local SQL database
* Designed to operate on a trusted network (for now), there's no message security, encryption, authentication currently.
* Netduino plus 2 firmware (ver 4.3.1) has a bug where trying to connect to a host that isn't responding freezes up the device (never times out). Working on isolating this seperately.
* This means a powercycle is necessary if the service goes down for any length of time.
