const {
    createPool
} = require('mysql2');

const pool = createPool({
    connectionLimit : 10,
    host: "localhost",
    user: "root",
    password: "Djq97hyy",
    database: "shoppinglist"
})
module.exports = pool;

function get_item(id){
    pool.query(`SELECT item FROM items S WHERE S.itID = ?`, [id], (err, result) =>{
        if (err) {
            return console.log(err);
        }
        console.log(result);
        return result;
    })
}

function get_price(id){
    pool.query(`SELECT price FROM items S WHERE S.itID = ?`, [id], (err, result) =>{
        if (err) {
            return console.log(err);
        }
        console.log(result);
        return result;
    })
}

function get_ID(name) {
    pool.query(`SELECT itID FROM items S WHERE S.item = ?`, [name], (err, result) =>{
        if (err) {
            return console.log(err);
        }
        console.log(result);
        return result;
    })
}

export function get_table_length() {
    pool.query(`SELECT * FROM items`, (err, result, fields) =>{
        if (err) {
            return console.log(err);
        }
        console.log(result.length);
        return result.length; 
    })
}



//get_item(3)
//get_price(3)
//get_ID("røv");
//get_table_length()