import os, glob
import csv
import pandas as pd

order_data = pd.read_csv("dataset/orders.csv", encoding='UTF8')
prior_data = pd.read_csv("dataset/order_products__prior.csv", encoding='UTF8')
print(order_data)

order_prior = pd.merge(prior_data,order_data,on=['order_id','order_id'])
order_prior = order_prior.sort_values(by=['user_id','order_id'])
print(order_prior)
