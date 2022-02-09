import React from 'react';

const CopyrightScript = () => (
    <script dangerouslySetInnerHTML={{
        __html: `
        const year = new Date().getFullYear();
        document.getElementById('copyright-end-year').innerHTML = year;
        `
    }} />
)

export default (
    <div className="content has-text-centered is-size-5">
        <p>
            The content of this website is copyright © 2021-<span id="copyright-end-year"/>
            <br />
            under the terms of the <a className="is-underlined" href="https://github.com/MangelMaxime/Nacara/blob/master/LICENSE.txt" style={{color: "white"}}>MIT License</a>
        </p>
        <CopyrightScript />
    </div>
)
