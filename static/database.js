const {
    createPool
} = require('mysql2');

const pool = createPool({
    host: "localhost",
    user: "root",
    password: "Djq97hyy",
    database: "shoppinglist"
})

pool.query(`SELECT * FROM items`, (err, result, fields) =>{
    if (err) {
        return console.log(err);
    }
    return console.log(result);
})