# rf-hackathon

## Running

Build the Docker image:
```bash
docker build . -t rf-hackathon
```
PS: te image creates a volume at `/output` that can be used to output the files to the host computer.

Run a container:
```bash
docker run -v `pwd`:/output rf-hackathon
```

Files will show up at your directory:
```bash
ls -ltr
-rw-r--r-- 1 user user  115888 Oct 13 19:08 sample-data.parquet
-rw-r--r-- 1 user user 1107560 Oct 13 19:08 sample-data.csv
-rw-r--r-- 1 user user  132973 Oct 13 19:08 sample-data.csv.gz
```
