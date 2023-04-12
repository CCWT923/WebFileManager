const host = "http://10.152.77.3:1000";

function getDriveList(){
    fetch(host + "/path/get/computer",{
        method:"GET"
    })
    .then(response=>response.json())
    .then(res=>{
        var drives = document.getElementById("drives");
        var fragment = document.createDocumentFragment();
        for(var i = 0; i < res.length; i++){
            var driveDiv = document.createElement("div");
            driveDiv.className = "drive";
            driveDiv.setAttribute("data-disk",res[i]["Name"].replace(":\\",""));
            var icon = document.createElement("i");
            if(res[i]["Type"] == "Network"){
                icon.className = "fa-sharp fas fa-network-wired";
            }else{
                icon.className = "fas fa-hard-drive";
            }
            var divInfoCon = document.createElement("div");
            divInfoCon.style.display = "flex";
            divInfoCon.style.setProperty("flex-direction","column");

            var divInfo1 = document.createElement("div");
            if(res[i]["Online"]){
                divInfo1.textContent = (res[i]["Label"] == "" ? "磁盘" : res[i]["Label"]) + " (" + res[i]["Name"].replace("\\","") + ")";
                var divInfo2 = document.createElement("div");
                divInfo2.textContent = res[i]["Format"];
            }else{
                divInfo1.textContent = "离线";
            }
            driveDiv.append(icon);
            divInfoCon.append(divInfo1);
            divInfoCon.append(divInfo2);
            driveDiv.append(divInfoCon);
            //添加事件处理程序
            driveDiv.addEventListener("click", (e)=>{
                navigateToDiskFile(e);
            });
            fragment.append(driveDiv);
        }
        drives.append(fragment);
    })
    .catch(err=>{
        alert(err);
    })
}

/**
 * 导航到指定的磁盘目录
 * @param {*} e 
 */
function navigateToDiskFile(e){
    var disk = e.currentTarget.getAttribute("data-disk");
    window.location.href = "./index.html?disk=" + disk;
}
/**
 * {
    "Name": "V:\\",
    "Format": "NTFS",
    "Label": "filesbackup",
    "Type": "Network",
    "Size": 4000617328640,
    "Free": 1945110904832
    }
 */