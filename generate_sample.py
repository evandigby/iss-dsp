import pandas as pd
import numpy as np

start = pd.Timestamp.utcnow()
end = start + pd.DateOffset(minutes=5)
TOTAL_SAMPLES = 10000
t = pd.to_datetime(np.linspace(start.value, end.value, TOTAL_SAMPLES))

# build the DataFrame
df = pd.DataFrame()
df['ts_start'] = t
df['ts_start'] = df.ts_start.astype('datetime64[s]')
df['ts_max'] = df.ts_start.apply(lambda x: x + pd.DateOffset(seconds=np.random.randint(10, 120)))
df['ts_end'] = df.ts_max.apply(lambda x: x + pd.DateOffset(seconds=np.random.randint(10, 120)))
df['frequency'] = df.ts_start.apply(lambda x: np.random.randint(800, 8000))
df['power'] = df.ts_start.apply(lambda x: np.random.randint(1, 10000))
df['latitude'] = 32.9002239196049
df['longitude'] = -96.96302798539023

# save the data in different formats
df.to_parquet('/output/sample-data.parquet', allow_truncated_timestamps=True)
df.to_csv('/output/sample-data.csv')
df.to_csv('/output/sample-data.csv.gz')
