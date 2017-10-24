var color_black   = '\u001b[30m';
var color_red     = '\u001b[31m';
var color_green   = '\u001b[32m';
var color_yellow  = '\u001b[33m';
var color_blue    = '\u001b[34m';
var color_magenta = '\u001b[35m';
var color_cyan    = '\u001b[36m';
var color_white   = '\u001b[37m';
var color_reset   = '\u001b[0m';
/*
 * Colored console log
 */
function cclog(msg, c) {
    console.log(c + msg + color_reset);
}

function clog(msg) { cclog(msg, color_cyan); }
function ylog(msg) { cclog(msg, color_yellow); }
//function blog(msg) { cclog(msg, color_blue); }
//function ylog(msg) { cclog(msg, color_yellow); }

exports.clog = clog;
exports.ylog = ylog;
