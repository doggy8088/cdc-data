# 疾病管制署資料開放平台 Mirror Site

本 Repo 目前僅同步 `*.csv` 資料集過來，每天 `7:00`, `12:00`, `17:00`, `22:00` 更新。

## 資料來源

[Taiwan CDC Open Data Portal](https://data.cdc.gov.tw/)

## 資料集統計數據

[DataSetStatistics](https://data.cdc.gov.tw/pages/datasetstatistics): Opendata 各資料集下載量、瀏覽量報表。

- CSV: <https://data.cdc.gov.tw/doc/OpdDataSetStatistics.csv>
- JSON: <https://data.cdc.gov.tw/doc/OpdDataSetStatistics.json>

取得單一資料集的 API 資訊：

- API: `https://data.cdc.gov.tw/api/3/action/package_show?id={dataset.資料集網址}`

    範例: <https://data.cdc.gov.tw/api/3/action/package_show?id=tmprescription>

## 資料下載


## 相關連結

- [資料集](https://data.cdc.gov.tw/dataset/)
- [開發人員](https://data.cdc.gov.tw/pages/developer)
  - OAS 3.0: <https://od.cdc.gov.tw/cdc/Ckan01.json>
  - [Swagger Editor](https://editor.swagger.io/)

---

最近更新時間: `2024-07-17 07:06:10`
