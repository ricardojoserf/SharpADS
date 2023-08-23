# SharpADS

C# program to write, read, delete or list Alternate Data Streams (ADS) within NTFS.


### Write one ADS value

Create or update and ADS value. The payload can be a string, hexadecimal value or a url to download a file:

```
SharpADS.exe write FILE_PATH STREAM_NAME PAYLOAD
```

String example:

```
SharpADS.exe write c:\Temp\test.txt ADS_name1 RandomString
```

Hexadecimal value example (payload starts with "0x..."):

```
SharpADS.exe write c:\Temp\test.txt ADS_name2 0x4142434445
```

Download file example (payload starts with "http" or "https"):

```
SharpADS.exe write c:\Temp\test.txt ADS_name3 http://127.0.0.1:8000/a.bin
```

![img](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/SharpADS-screenshots/Screenshot_1.png)


### Read one ADS value

```
SharpADS.exe read FILE_PATH STREAM_NAME
```

Read ADS example:

```
SharpADS.exe read c:\Temp\test.txt ADS_name1
```

![img](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/SharpADS-screenshots/Screenshot_2.png)


### Delete one ADS value

```
SharpADS.exe delete FILE_PATH STREAM_NAME
```

Delete ADS example:

```
SharpADS.exe delete c:\Temp\test.txt ADS_name1
```

![img](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/SharpADS-screenshots/Screenshot_3.png)


### List all ADS values

```
SharpADS.exe list FILE_PATH
```

List all ADS values:

```
SharpADS.exe list c:\Temp\test.txt
```

![img](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/SharpADS-screenshots/Screenshot_4.png)


### Clear all ADS values

```
SharpADS.exe clear FILE_PATH
```

Clear all ADS values:

```
SharpADS.exe clear c:\Temp\test.txt
```

![img](https://raw.githubusercontent.com/ricardojoserf/ricardojoserf.github.io/master/images/SharpADS-screenshots/Screenshot_5.png)



--------------------------------------------------------

### Credits

This is based on C++ code from Sektor7's [Malware Development Advanced - Vol.1 course](https://institute.sektor7.net/rto-maldev-adv1).
