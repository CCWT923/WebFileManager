const host = "http://127.0.0.1:1000";
let disk = "";
/**
 * 当前所在的路径
 */
let currentPath = "";

function init(){
    var urlParam = new URLSearchParams(window.location.search);
    if(urlParam.has("disk")){
        disk = urlParam.get("disk");
        listRootDir();
    }else{
        alert("没有指定磁盘");
    }
}

/**
 * 展开根目录
 */
function listRootDir(){
    if(disk == ""){
        alert("没有指定磁盘");
        return;
    }
    currentPath = disk + ":\\";
    fetch(host + "/path/get?target=" + disk + ":\\",{
        method:"GET"
    })
    .then(response=>{
        if(response.status == 200){
            return response.text();
        }else{
            return undefined;
        }
    })
    .then(res=>{
        if(res == undefined){
            alert("打开文件或目录失败！");
            return;
        }
        try{
            var obj = JSON.parse(res);
            if(obj["Code"] == 0){
                listFiles(obj["Items"]);
            }
        }catch(ex){
            console.log(ex);
        }
    })
    .catch(err=>{
        alert(err);
    })
}

/**
 * 返回主页
 */
function navigateToHome(){
    window.location.href = "./home.html";
}

/**
 * 处理文件或文件夹
 * @param {Element} e 事件源
 * @returns 
 */
function handleItem(e){
    var t = e.currentTarget.getAttribute("data-t");
    var path = e.currentTarget.getAttribute("data-path");

    if (t == 'f'){
        //处理文件
        downloadFile(path);
    }
    else if(t == 'd'){
        //处理目录
        getFolder(path);
    }else if(t == 'a'){
        //动作
        getFolder(currentPath, path);
    }else{
        alert("类型未定义");
        return;
    }
}

/**
 * 下载文件
 * @param {String} name 文件路径
 */
function downloadFile(name){
    let filename = "";
    fetch(host + "/path/get?target=" + name, {
        method: "GET"
    })
    .then(response=>{
        var header = response.headers.get('content-disposition');
        filename = header.split(';')[1].split('=')[1].trim();
        return response.blob();
    })
    .then(res=>{
        var url = URL.createObjectURL(res);
        var link = document.createElement("a");
        link.href = url;
        if(filename == ""){
            filename = getFileName(name);
        }
        link.download = filename;
        link.click();
    })
    .catch(err=>{
        alert(err);
    })
}

/**
 * 请求指定目录中的所有文件/目录
 * @param {String} name 目录名
 */
function getFolder(name, action = ""){
    let address = "";
    if(action == ""){
        address = host + "/path/get?target=" + name;
    }else{
        address = host + "/action/" + action + "?current=" + name;
    }

    fetch(address, {
        method:"GET"
    })
    .then(response=>response.text())
    .then(res=>{
        var obj = JSON.parse(res);
        if(obj["Code"] == 0){
            currentPath = obj["Dir"];
            listFiles(obj["Items"]);
            
        }else{
            alert(obj["Message"]);
        }
    })
    .catch(err=>{
        alert(err);
    })
}

/**
 * 从路径中取出文件名/目录名
 * @param {String} path 路径
 * @returns 
 */
function getFileName(path){
    return path.substring(path.lastIndexOf("\\") + 1);
}

/**
 * 列出数组中的所有文件和目录
 * @param {Array} files 包含文件信息的数组
 */
function listFiles(files){
    var links = document.getElementById("links");
    links.textContent = "";
    var fragment = document.createDocumentFragment();
    //返回上一级的按钮
    var back = document.createElement("div");
    back.setAttribute("data-t", "a");
    back.setAttribute("data-path","up");
    var i1 = document.createElement("i");
    i1.className = "fas fa-long-arrow-up file";
    back.append(i1);
    back.append("返回上层");
    back.addEventListener("click", (e)=>{handleItem(e)});
    fragment.append(back);

    for(var i = 0; i < files.length; i++){
        var div = document.createElement("div");
        div.className = "file-item";
        div.setAttribute("data-path", files[i]["Name"]);
        if(files[i]["IsFile"]){
            div.setAttribute("data-t", "f");
            var sp = document.createElement("span");
            sp.textContent = files[i]["Size"] + files[i]["Unit"];
            sp.className = "file-size";
            var icon = document.createElement("i");
            icon.className = "fas fa-file icon-file";
            div.append(icon);
            div.append(getFileName(files[i]["Name"]));
            div.append(sp);
        }
        else{
            div.setAttribute("data-t", "d");
            var icon = document.createElement("i");
            icon.className = "fas fa-folder icon-folder";
            div.append(icon);
            div.append(getFileName(files[i]["Name"]));
        }
        div.addEventListener("click", (e)=>{handleItem(e)});
        fragment.appendChild(div);
    }
    links.append(fragment);
}
