window.forma = {
    read: () => localStorage.getItem("forma.blazor.v1"),
    write: json => localStorage.setItem("forma.blazor.v1", json),
    localDate: () => {
        const now = new Date();
        const two = value => String(value).padStart(2, "0");
        return [now.getFullYear(), two(now.getMonth() + 1), two(now.getDate())].join("-");
    },
    download: (name, json) => {
        const url = URL.createObjectURL(new Blob([json], { type: "application/json" }));
        const link = document.createElement("a");
        link.href = url;
        link.download = name;
        document.body.append(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 2000);
    },
    showDialog: dialog => { if (!dialog.open) dialog.showModal(); },
    closeDialog: dialog => { if (dialog.open) dialog.close(); }
};
