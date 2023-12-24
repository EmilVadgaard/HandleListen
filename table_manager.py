import mysql.connector

db = mysql.connector.connect(
    host='localhost',
    user='root',
    passwd='Djq97hyy',
    database='shoppinglist'
    )

mycursor = db.cursor()

#mycursor.execute("CREATE TABLE items (itID int PRIMARY KEY AUTO_INCREMENT, item VARCHAR(255) UNIQUE, price float)")

def insert_items(item: str, price: float) -> None:
    mycursor.execute("INSERT INTO items (item, price) VALUES (%s,%s)", (item, price))
    db.commit()

def insert_items_ID(ID: int, item: str, price: float) -> None:
    mycursor.execute("INSERT INTO items (itID, item, price) VALUES (%s,%s,%s)", (ID, item, price))
    db.commit()

def delete_items(name: str) -> None:
    #temp = [name]
    mycursor.execute("DELETE FROM items S WHERE S.item = %s", ([name]))
    db.commit()

def show_items() -> None:
    mycursor.execute("SELECT * FROM items")
    for x in mycursor:
        print(x)