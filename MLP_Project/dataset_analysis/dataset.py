import os, glob
import csv
import pandas as pd
'''
data1 = pd.read_csv("dataset/products.csv", encoding='UTF8')
dataframe1 = data1.loc[:,["product_id","aisle_id"]] # Only Product_id & aisle_id
dataframe1 = dataframe1.sort_values(by="aisle_id")
dataframe1.columns = ['product_id','aisle_id']
print(dataframe1)
'''
data = pd.read_csv("dataset/products_new.csv", encoding='UTF8')
dataframe_fixed = pd.DataFrame(index=range(1,1250),columns=range(1,135))
row_num = 0
row_change = 0
print(dataframe_fixed)
print(data["aisle_id"][0])
for i in range(len(data)-1):
    if(data["aisle_id"][i] == row_num+1):
        print("i: ",i)
        dataframe_fixed[row_num+1][i-row_change-1] = data["product_id"][i]
        print(row_num)
    elif(row_num+2 == 135):
        break
    else:
        dataframe_fixed[row_num+2][0] = data["product_id"][i]
        row_num += 1
        row_change = i
dataframe_T = dataframe_fixed.T
# dataframe1.to_csv("dataset/products_new.csv", index=False)
dataframe_fixed.to_csv("dataset/products_final.csv", index=False)
dataframe_T.to_csv("dataset/products_final_T.csv")