function downloadRequest(event) {
  event.preventDefault();

  let requestIdInput = document.getElementById("request-id");
  let requestRequestIdInput = document.getElementById("request-request-id");

  let dataShareRequestId = requestIdInput.value;
  let dataShareRequestRequestId = requestRequestIdInput.value;

  window.open(`/dataRequest/DownloadCompletedRequest?requestId=${dataShareRequestId}&requestRequestId=${dataShareRequestRequestId}`);
}

document.getElementById('download-request-control').addEventListener('click', downloadRequest);