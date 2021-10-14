import pandas as pd
import numpy as np

start = pd.Timestamp.utcnow()
end = start + pd.DateOffset(minutes=5)
TOTAL_SAMPLES = 10000
t = pd.to_datetime(np.linspace(start.value, end.value, TOTAL_SAMPLES))

# build the DataFrame
df = pd.DataFrame()
df['ts_seed'] = t
df['ts_seed'] = df.ts_seed.astype('datetime64[ms]')
df['ts_start'] = df.ts_seed.apply(lambda x: x + pd.DateOffset(microseconds=np.random.randint(10000000, 120000000)))
df['ts_max'] = df.ts_start.apply(lambda x: x + pd.DateOffset(microseconds=np.random.randint(10000000, 120000000)))
df['ts_end'] = df.ts_max.apply(lambda x: x + pd.DateOffset(microseconds=np.random.randint(10000000, 120000000)))
df['frequency'] = df.ts_seed.apply(lambda x: np.random.randint(800, 8000))
df['power'] = df.ts_seed.apply(lambda x: np.random.randint(1, 10000))
df = df.drop(columns=['ts_seed'])

# print sample data
print(df.info(verbose=True))
print('Sample Data:')
print(df.head())

# save the data in different formats
df.to_parquet('/output/sample-data.parquet', index=False, allow_truncated_timestamps=True)
df.to_csv('/output/sample-data.csv', index=False, date_format='%Y-%m-%dT%H:%M:%S.%f%z')
df.to_csv('/output/sample-data.csv.gz', index=False, date_format='%Y-%m-%dT%H:%M:%S.%f%z')
