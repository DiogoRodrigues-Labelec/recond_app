DISTRIBUTION FILE CONTENTS

bmips34k.bin            BIOS 02.04

rs3g.bin                C.I.T. version 11.01.10.45.03 STANDARD
                        for RS123/RS353/Connect-RS/Regesta-Smart/H2AUTO-Compact series

fw00001c.bfw            Lantiq VRX288 VDSL2/ADSL POTS v2.0
fw00001d.bfw            Lantiq VRX288 VDSL2/ADSL ISDN v1.0

version_map.txt         Help file for the selection of the suitable .bin for your device

mibs.tgz                MIB files and MIBS_Finder application

dictionary.txt          Radius Dictionary extensions

els.rtf                 Event Logging System Manual

v1101104503.md5             MD5 checksum of binary files


SOFTWARE UPDATE VIA FTP

    1)  Extract the files included in the distribution to a temporal directory
        (this procedure guarantees software integrity)

    2)  Read the file "readme.txt" and find out:
            - the firmwares your router needs
            - the CIT version suitable for your router model

    3)  Establish FTP connection with the router
    4)  Configure BINARY mode, and optionally, HASH marking

    5)  Upload the BIOS file:               put bios.bin
    6)  Store the BIOS file:                quote site savebuffer
    7)  Wait until finished

    8)  Upload the required firmware file:  put fw00000x.bfw
    9)  Store the firmware file:            quote site savebuffer
    10) Wait
    11) Repeat this process for each firmware your router needs

            NOTE: Do not rename the firmware files or the router will not
                  find them during the boot process

    12) Upload the suitable CIT version:    put <cit file>
    13) Store the CIT file:                 quote site savebuffer
    14) Wait

            NOTE: It is recommended to have only one CIT version in the
                  Flash disc to avoid memory exhaustion. Upload the CIT file
                  with the name of the current CIT file in Flash.

            NOTE: If the upload process fails in the "put" stage, activate
                  the direct mode with "quote site direct on" and then repeat
                  the transfer: the file will be stored in Flash directly during
                  the transfer, so you must check it completes successfully.

    15) Reload the router and check the router is running the BIOS and CIT
        versions you expect, with the terminal command: "p 3, configuration"



SOFTWARE UPDATE VIA X-MODEM

    1)  Extract the files included in the distribution to a temporal directory
        (this procedure guarantees software integrity)

    2)  Read the file "readme.txt" and find out:
            - the firmwares your router needs
            - the CIT version suitable for your router model

    3)  Connect a console terminal (9600-8-N-1 without flow control) and power
        up the router

    4)  When the router has dumped ">>" and dots begin to appear, press CTRL-T
        to stop the boot process

    5)  The menu option "x" allows you to perform X-Modem uploads

    6)  By default, the transfer will be done at 115200 bps, and restored to
        9600 when the transfer finishes. The transferred file has been saved
        when the first, fifth and sixth leds are green.

    7)  Upload the BIOS file via X-Modem (it does not matter the name because
        it is recognized as a BIOS and saved correctly).

    8)  Upload the firmwares: you must configure the router X-Modem process to
        save the file with the same name of the firmware to be uploaded.

    9)  Upload the suitable CIT version: you must configure the router X-Modem
        process to save the file with the same name of the current CIT file

    10) Reload the router and check the router is running the BIOS and CIT
        versions you expect, with the terminal command: "p 3, configuration"



[BIOS]  Basic Input Output System
[CIT]   Codename: InTernetworking









