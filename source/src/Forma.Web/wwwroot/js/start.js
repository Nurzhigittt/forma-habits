(() => {
    const showFailure = () => {
        const error = document.getElementById("blazor-error-ui");
        error.setAttribute("role", "alert");
        error.style.display = "block";
    };
    if (window.Blazor) window.Blazor.start().catch(showFailure);
    else showFailure();
})();
