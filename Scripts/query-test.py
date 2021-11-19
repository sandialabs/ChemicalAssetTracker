import sys
import csv
import random
import pymysql
import time

db = pymysql.connect(host='localhost', user='cms', passwd='cms', db='cms')
cur = db.cursor(pymysql.cursors.DictCursor)

def run_query(query):
    start_time = time.time()
    cur.execute(query)
    row = cur.fetchone()
    count = 0
    while row:
        count += 1
        row = cur.fetchone()
    elapsed_time = time.time() - start_time
    print(f'Retrieved {count} rows in {elapsed_time:1.2f} seconds')

while True:
    ans = input("Enter query or 'quit': ")
    if ans == 'quit':
        break
    else:
        try:
            run_query(ans)
        except BaseException as error:
            print(error)




