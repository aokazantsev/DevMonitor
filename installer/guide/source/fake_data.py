import datetime
import math
import os
import random

base = os.path.dirname(os.path.abspath(__file__))
data = os.path.join(base, "data")
os.makedirs(data, exist_ok=True)
for name in os.listdir(data):
    os.remove(os.path.join(data, name))

keys = ["cpu_load", "cpu_temp", "gpu_load", "gpu_temp", "ram", "vram", "jvm_gb", "gradle_gb", "kotlin_gb", "workers_gb"]
header = "time," + ",".join(k + "_avg," + k + "_max" for k in keys)
today = datetime.date.today()
random.seed(7)

for back in [0] + list(range(1, 11)) + [17, 18, 20]:
    day = today - datetime.timedelta(days=back)
    if day.weekday() == 6 and back < 17:
        continue
    lines = [header]
    start = 9 * 60 + random.randint(0, 60)
    end = 18 * 60 + random.randint(0, 120)
    if back == 0:
        now = datetime.datetime.now()
        start = 8 * 60 + 3
        end = now.hour * 60 + now.minute - 1
    for minute in range(start, end):
        if 13 * 60 <= minute < 14 * 60:
            continue
        building = math.sin(minute / 17.0) > 0.6
        cpu = 85 + random.random() * 14 if building else 8 + random.random() * 15
        cpu_temp = 70 + cpu * 0.25 + random.random() * 4
        gpu = 5 + random.random() * 20
        gpu_temp = 33 + gpu * 0.4
        ram = 45 + (20 if building else 0) + random.random() * 5
        vram = 14 + random.random() * 4
        gradle = 5 + random.random() * 2 if building else 2 + random.random() * 0.5
        kotlin = 3 + random.random() if building else 1 + random.random() * 0.3
        workers = 1 + random.random() * 1.5 if building else 0
        jvm = gradle + kotlin + workers
        values = [cpu, min(100, cpu + 8), cpu_temp, cpu_temp + 6, gpu, gpu + 10, gpu_temp, gpu_temp + 3, ram, ram + 1, vram, vram + 1,
                  jvm, jvm + 0.5, gradle, gradle + 0.2, kotlin, kotlin + 0.2, workers, workers + 0.3]
        lines.append("%02d:%02d," % (minute // 60, minute % 60) + ",".join("%.2f" % v for v in values))
    with open(os.path.join(data, day.isoformat() + ".csv"), "w", newline="\n", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
print(sorted(os.listdir(data)))
