// 简单用户数据（演示用）
const users = {
    "visitor": {password:"123", role:"visitor"},
    "coworker": {password:"123", role:"worker"}
};

function login(){
    const u = document.getElementById("username").value;
    const p = document.getElementById("password").value;

    if(users[u] && users[u].password === p){
        localStorage.setItem("role", users[u].role);
        window.location.href = "index.html";
    }else{
        alert("登录失败");
    }
}

function logout(){
    localStorage.clear();
    window.location.href = "login.html";
}

function showSection(id){
    document.querySelectorAll(".section").forEach(s=>s.style.display="none");
    document.getElementById(id).style.display="block";
}

// 权限控制
window.onload = function(){
    const role = localStorage.getItem("role");
    if(!role){
        window.location.href = "login.html";
        return;
    }

    if(role === "visitor"){
        document.getElementById("personal").style.display = "none";
    }

    showSection("gospel");
}

// 留言功能
function postMsg(){
    const text = document.getElementById("msg").value;
    if(!text) return;

    const div = document.createElement("div");
    div.innerText = text;
    document.getElementById("board").appendChild(div);
}
