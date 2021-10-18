# RF Scan in Space Code

Scan radio frequency emissions in the International Space Station.

Inspired by, and tested on, [the Azure Mock Space Station](https://github.com/Azure/mock-spacestation).

## Description

Radio emissions can be monitored with an RF receiver on mount of the International Space Station (ISS). The goal of the project is to enable better space exploration, and safety mechanisms, to further benefit the ISS’ mission of bringing benefits to people on Earth.

Emissions raw data can be ingested and processed by a python module which would be exported in a Docker container to run in the onboard HP Spaceboard computer-2. The compute needs to be done onboard the ISS because the required sample rates for these signals is high. If there is special interest in a specific frequency, the on-board computer can filter out the noise and keep logs of such signal. Radio sources can be natural or man-made. Logs processed will be time, frequency, power and ISS position.

## Inspiration

Space travel will become more usual in the future. There are multiple applications using radio signals that travel and it would be interesting to see how we can manage that computed data in Azure.

## How It Will Work / How It Will Be Built

Process is as follows:

1. HP Spaceboard computer-2 aboard the ISS will run a Python module containerized in Docker (<1 GB) and read raw signal input from an RF receiver, apply a Fourier transform and output information of frequencies. Docker container should run with minimum human interaction. The program will have previous knowledge of which frequency is of interest, remove the noise, calculate the power and save logs in a Parquet format.
1. Data from the ISS will be downloaded to ground station in a small transfer window: 2 Mbit/sec, two hours a week.
1. Azure Storage can get the log file from ground station using AzCopy. A Logic app can identify when a new file has been uploaded and send an email notification from an Office 365 mailbox to team members and turn-on an IoT light bulb.
1. Data Factory can orchestrate and transform data from Parquet to CSV.
1. Azure Function picks up the log time and returns the ISS location at the time when the frequency was detected. This information will be written back to the CSV.
1. SQL server can create tables using the CSV file.
1. Power BI can present reports to internal users and embed reports so they can be used by other apps.
1. Azure Web App can publish some reports and have 3D visualizations in a .NET code 5 site.

_Note: we are generating mock data for the RF receiver hardware._

## Code

- The [iis-code](./iis-code) folder contains the Python code and Dockerfiles that run on the mock space station.
- The [visualization](./visualization) folder contains the C#/JavaScript code used to generate the mock 3d visualization.