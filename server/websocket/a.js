var fs = require('fs');

function isExistFile(file) { 
    try { 
        fs.statSync(file); 
        return true; 
    } catch(err) { 
        if(err.code === 'ENOENT') return false; 
    } 
} 

if(isExistFile("data/worked3D.txt")) {
    console.log("yes");
}
else {
    fs.writeFile("data/worked3D.txt", "", function(err) {
        if (err) { throw err; }
    });
    console.log("no");
}
