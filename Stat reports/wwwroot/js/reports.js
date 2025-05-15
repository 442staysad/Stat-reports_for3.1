document.getElementById("summaryForm").addEventListener("submit", async function (e) {
    e.preventDefault();

    let branches = [...document.getElementById("branchesSelect").selectedOptions].map(o => o.value);
    let period = document.getElementById("periodInput").value + "-01";

    let response = await fetch(`/Reports/summary?branchIds=${branches.join(",")}&period=${period}`);
    let data = await response.json();

    let tableBody = document.querySelector("#summaryTable tbody");
    tableBody.innerHTML = "";

    data.branchSummaries.forEach(row => {
        let tr = document.createElement("tr");
        tr.innerHTML = `<td>${row.branchName}</td><td>${row.totalAmount}</td>`;
        tableBody.appendChild(tr);
    });
});