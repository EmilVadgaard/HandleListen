const {
    createPool
} = require('mysql');

const pool = createPool({
    host: "localhost",
    user: "roos",
    password: "Djq97hyy",
    database: "shoppinglist"
})

pool.query(`SELECT * FROM items`, (err, result, fields) =>{
    if (err) {
        return console.log(err);
    }
    return console.log(result);
})